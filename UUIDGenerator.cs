/* JUG Java Uuid Generator
 *
 * Copyright (c) 2002,2010 Tatu Saloranta, tatu.saloranta@iki.fi, Tommi S.E. Laukkanen, tommi.s.e.laukkanen@gmail.com
 *
 * Licensed under the License specified in the file LICENSE which is
 * included with the source code.
 * You may not use this file except in compliance with the License.
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace Jug
{

    /**
     * UUIDGenerator is the class that contains factory methods for
     * generating UUIDs using one of the three specified 'standard'
     * UUID generation methods:
     * (see <a href="http://www.opengroup.org/dce/info/draft-leach-uuids-guids-01.txt">draft-leach-uuids-guids-01.txt</a> for details)
     * <ul>
     * <li>Time-based generation generates UUID using spatial and
     *     temporal uniqueness. Spatial uniqueness is derived from
     *     ethernet address (MAC, 802.1); temporal from system clock.
     *     See the details from the explanation of
     *     {@link #generateTimeBasedUUID} function.
     * <li>Name-based method uses MD5 hash (or, optionally any user-specified
     *     digest method) of the string formed from
     *     a name space and name.
     * <li>Random method uses Java2 API's SecureRandom to produce
     *     cryptographically secure UUIDs.
     * <li>Tag URI - method uses a variation of name-based method; instead of
     *    using a name space UUID and name, a hash (MD5 by default) is calculated
     *    from URI-tag-prefix, 2 obligatory strings (URL, path) and one
     *    optional string (current date). The resulting UUID is still considered
     *    to be 'name-based UUID' as the specification does not have additional
     *    UUID type ids available.
     *    Note that this is a non-standard method and not strictly UUID-'standard'
     *    compliant.
     * </ul>
     *
     * Some comments about performance:
     * <ul>
     * <li>For non-performance critical generation, all methods with default
     *    arguments (default random number generator, default hash algorithm)
     *    should do just fine.
     * <li>When optimizing performance, it's better to use explicit random
     *    number generator and/or hash algorithm; this way global instance
     *    sharing need not be synchronized
     * <li>Which of the 3 methods is fastest? It depends, and the best way
     *    is to just measure performance, discarding the first UUID generated
     *    with the methods. With time-based method, main overhead comes from
     *    synchronization, with name-based (MD5-)hashing, and with random-based
     *    the speed of random-number generator. Additionally, all methods may
     *    incur some overhead when using the shared global random nunber
     *    generator or hash algorithm.
     * <li>When generating the first UUID with random-/time-based methods,
     *    there may be noticeable delay, as the random number generator is
     *    initialized. This can be avoided by either pre-initialising the
     *    random number generator passed (with random-based method), or by
     *    generating a dummy UUID on a separate thread, when starting a
     *    program needs to generate UUIDs at a later point.
     *    
     * </ul>
     */
    public class UUIDGenerator
    {
        private static UUIDGenerator sSingleton = new UUIDGenerator();

        /**
         * Random-generator, used by various UUID-generation methods:
         */
        private Random mRnd = null;

        // Ethernet address for time-based UUIDs:

        private Object mDummyAddressLock = new Object();
        private PhysicalAddress mDummyAddress = null;
        private Object mTimerLock = new Object();
        private UUIDTimer mTimer = null;

        /**
         * MD5 hasher for name-based digests:
         */
        private MD5 mHasher = null;

        /*
        /////////////////////////////////////////////////////
        // Life-cycle
        /////////////////////////////////////////////////////
         */

        /**
         * Constructor is private to enforce singleton access.
         */
        private UUIDGenerator() { }

        /**
         * Method used for accessing the singleton generator instance.
         */
        public static UUIDGenerator Current
        {
            get
            {
                return sSingleton;
            }
        }

        /**
         * Method that can (and should) be called once right after getting
         * the instance, to ensure that system time stamp values used are
         * valid (with respect to values used earlier by JUG instances), and
         * to use file-lock based synchronization mechanism to prevent multiple
         * JVMs from running conflicting instances of JUG (first one to be
         * started wins on contention). It can also be called to stop
         * synchronization by calling it with argument null, although such
         * usage is strongly discouraged (ie. it's a good idea to either never
         * use synchronization, or always; but not to mix modes).
         *<p>
         * Caller needs to instantiate an instance of
         * {@link TimestampSynchronizer}; currently the only standard
         * implementation is
         * {@link org.safehaus.uuid.ext.FileBasedTimestampSynchronizer} (which
         * is JDK 1.4+).
         *<p>
         * Note: since the generator instance is a singleton, calling this
         * method will always cause all generation to be synchronized using
         * the specified method.
         *
         * @param sync Synchronizer instance to use for synchronization.
         */

        public void SynchronizeExternally(TimestampSynchronizer sync)
        {
            lock (mTimerLock)
            {
                if (mTimer == null)
                {
                    mTimer = new UUIDTimer(GetRandomNumberGenerator());
                }
                mTimer.SetSynchronizer(sync);
            }
        }

        /*
        /////////////////////////////////////////////////////
        // Configuration
        /////////////////////////////////////////////////////
         */

        /**
         * Method that returns a randomly generated dummy ethernet address.
         * To prevent collision with real addresses, the returned address has
         * the broadcast bit set, ie. it doesn't represent address of any existing
         * NIC.
         *
         * Note that this dummy address will be shared for the lifetime of
         * this UUIDGenerator, ie. only one is ever generated independent of
         * how many times this methods is called.
         *
         * @return Randomly generated dummy ethernet broadcast address.
         */
        public PhysicalAddress GetDummyAddress()
        {
            lock (mDummyAddressLock)
            {
                if (mDummyAddress == null)
                {
                    Random rnd = GetRandomNumberGenerator();
                    byte[] dummy = new byte[6];
                    rnd.NextBytes(dummy);
                    /* Need to set the broadcast bit to indicate it's not a real
                     * address.
                     */
                    /* 08-Feb-2004, TSa: Note: it's the least bit, not highest;
                     *   thanks to Ralf S. Engelschall for fix:
                     */
                    dummy[0] |= (byte)0x01;
                    mDummyAddress = new PhysicalAddress(dummy);
                }
            }

            return mDummyAddress;
        }

        /**
         * Method for getting the shared random number generator used for
         * generating the UUIDs. This way the initialization cost is only
         * taken once; access need not be synchronized (or in cases where
         * it has to, SecureRandom takes care of it); it might even be good
         * for getting really 'random' stuff to get shared access...
         */
        public Random GetRandomNumberGenerator()
        {
            /* Could be synchronized, but since side effects are trivial
             * (ie. possibility of generating more than one SecureRandom,
             * of which all but one are dumped) let's not add synchronization
             * overhead:
             */
            if (mRnd == null)
            {
                mRnd = new Random(Environment.TickCount);
            }
            return mRnd;
        }

        /**
         * Method that can  be called to specify alternative random
         * number generator to use. This is usually done to use
         * implementation that is faster than
         * {@link SecureRandom} that is used by default.
         *<p>
         * Note that to avoid first-time initialization penalty
         * of using {@link SecureRandom}, this method has to be called
         * before generating the first random-number based UUID.
         */
        public void SetRandomNumberGenerator(Random r)
        {
            mRnd = r;
        }

        /* Method for getting the shared message digest (hash) algorithm.
         * Whether to use the shared one or not depends; using shared instance
         * adds synchronization overhead (access has to be sync'ed), but
         * using multiple separate digests wastes memory.
         */
        public MD5 GetHashAlgorithm()
        {
            /* Similar to the shared random number generator, it's not necessary
             * to synchronize initialization. However, use of the hash instance
             * HAS to be synchronized by the caller to prevent problems with
             * multiple threads updating digest etc.
             */
            if (mHasher == null)
            {
                mHasher = new MD5CryptoServiceProvider();
            }
            return mHasher;
        }

        /*
        /////////////////////////////////////////////////////
        // UUID generation methods
        /////////////////////////////////////////////////////
         */

        /**
         * Method for generating (pseudo-)random based UUIDs, using the
         * default (shared) SecureRandom object.
         * 
         * Note that the first time
         * SecureRandom object is used, there is noticeable delay between
         * calling the method and getting the reply. This is because SecureRandom
         * has to initialize itself to reasonably random state. Thus, if you
         * want to lessen delay, it may be be a good idea to either get the
         * first random UUID asynchronously from a separate thread, or to
         * use the other generateRandomBasedUUID passing a previously initialized
         * SecureRandom instance.
         *
         * @return UUID generated using (pseudo-)random based method
         */
        public UUID GenerateRandomBasedUUID()
        {
            return GenerateRandomBasedUUID(GetRandomNumberGenerator());
        }

        /**
         * Method for generating (pseudo-)random based UUIDs, using the
         * specified  SecureRandom object. To prevent/avoid delay JDK's
         * default SecureRandom object causes when first random number
         * is generated, it may be a good idea to initialize the SecureRandom
         * instance (on a separate thread for example) when app starts.
         * 
         * @param randomGenerator Random number generator to use for getting the
         *   random number from which UUID will be composed.
         *
         * @return UUID generated using (pseudo-)random based method
         */
        public UUID GenerateRandomBasedUUID(Random randomGenerator)
        {
            byte[] rnd = new byte[16];

            randomGenerator.NextBytes(rnd);

            return new UUID(UUID.TYPE_RANDOM_BASED, rnd);
        }

        /**
         * Method for generating time based UUIDs. Note that this version
         * doesn't use any existing Hardware address (because none is available
         * for some reason); instead it uses randomly generated dummy broadcast
         * address.
         *<p>
         * Note that since the dummy address is only to be created once and
         * shared from there on, there is some synchronization overhead.
         *
         * @return UUID generated using time based method
         */
        public UUID GenerateTimeBasedUUID()
        {
            return GenerateTimeBasedUUID(GetDummyAddress());
        }

        /**
         * Method for generating time based UUIDs.
         * 
         * @param addr Hardware address (802.1) to use for generating
         *   spatially unique part of UUID. If system has more than one NIC,
         *   any address is usable. If no NIC is available (or its address
         *   not accessible; often the case with java apps), a randomly
         *   generated broadcast address is acceptable. If so, use the
         *   alternative method that takes no arguments.
         *
         * @return UUID generated using time based method
         */
        public UUID GenerateTimeBasedUUID(PhysicalAddress addr)
        {
            byte[] contents = new byte[16];

            addr.GetAddressBytes().CopyTo(contents, 10);

            lock (mTimerLock)
            {
                if (mTimer == null)
                {
                    mTimer = new UUIDTimer(GetRandomNumberGenerator());
                }

                mTimer.GetTimestamp(contents);
            }

            return new UUID(UUID.TYPE_TIME_BASED, contents);
        }

        /**
         * Method for generating name-based UUIDs, using the standard
         * name-based generation method described in the UUID specs,
         * and the caller supplied hashing method.
         *
         * Note that this method is not synchronized, so caller has to make
         * sure the digest object will not be accessed from other threads.
         *
         * Note that if you call this method directly (instead of calling
         * the version with one less argument), you have to make sure that
         * access to 'hash' is synchronized; either by only generating UUIDs
         * from one single thread, or by using explicit sync'ing.
         * 
         * @param nameSpaceUUID UUID of the namespace, as defined by the
         *   spec. UUID has 4 pre-defined "standard" name space strings
         *   that can be passed to UUID constructor (see example below).
         *   Note that this argument is optional; if no namespace is needed
         *   (for example when name includes namespace prefix), null may be
         *   passed.
         * @param name Name to base the UUID on; for example,
         *   IP-name ("www.w3c.org") of the system for UUID.NAMESPACE_DNS,
         *   URL ("http://www.w3c.org/index.html") for UUID.NAMESPACE_URL
         *   and so on.
         * @param digest Instance of MessageDigest to use for hashing the name
         *   value. hash.reset() will be called before calculating the has
         *   value, to make sure digest state is not random and UUID will
         *   not be randomised.
         *
         * @return UUID generated using name-based method based on the
         *   arguments given.
         *
         * Example:
         *   <code>
         *      UUID uuid = gen.generateNameBasedUUID(
         *         new UUID(UUID.NAMESPACE_DNS, "www.w3c.org"));
         *   </code>
         */
        public UUID GenerateNameBasedUUID(UUID nameSpaceUUID, String name,
                                          MD5 digest)
        {
            if (name == null)
            {
                throw new NullReferenceException("Name parameter was null.");
            }
            digest.Initialize();
            byte[] hash;
            if (nameSpaceUUID != null)
            {
                byte[] a1 = nameSpaceUUID.ToByteArray();
                byte[] a2 = Encoding.UTF8.GetBytes(name);
                byte[] rv = new byte[a1.Length + a2.Length];
                System.Buffer.BlockCopy(a1, 0, rv, 0, a1.Length);
                System.Buffer.BlockCopy(a2, 0, rv, a1.Length, a2.Length);

                hash = digest.ComputeHash(rv);
            }
            else
            {
                hash = digest.ComputeHash(Encoding.UTF8.GetBytes(name));
            }
            return new UUID(UUID.TYPE_NAME_BASED, hash);
        }

        /**
         * Method similar to the previous one; the difference being that a
         * shared MD5 digest instance will be used. This also means that there is
         * some synchronization overhead as MD5-instances are not thread-safe
         * per se.
         */
        public UUID GenerateNameBasedUUID(UUID nameSpaceUUID, String name)
        {
            if (name == null)
            {
                throw new NullReferenceException("Name parameter was null.");
            }
            MD5 hasher = GetHashAlgorithm();
            lock (hasher)
            {
                return GenerateNameBasedUUID(nameSpaceUUID, name, GetHashAlgorithm());
            }
        }

        /**
         * Method for generating UUIDs using tag URIs. A hash is calculated from
         * the given tag URI (default being MD5 hash). The resulting UUIDs
         * are reproducible, ie. given the same tag URI, same UUID will always
         * result, much like with the default name-based generation method.
         *
         * Note that this a non-standard way of generating UUIDs; it will create
         * UUIDs that appear to be name-based (and which are, but not using the
         * method specified in UUID specs).
         *
         * @param name tag URI to base UUID on.
         */
        public UUID GenerateTagURIBasedUUID(TagURI name)
        {
            return GenerateNameBasedUUID(null, name.ToString());
        }

        /**
         * Method for generating UUIDs using tag URIs. A hash is calculated from
         * the given tag URI using the specified hashing algorith,.
         * The resulting UUIDs are reproducible, ie. given the same tag URI and
         * hash algorithm, same UUID will always result, much like with the
         * default name-based generation method.
         *
         * Note that this a non-standard way of generating UUIDs; it will create
         * UUIDs that appear to be name-based (and which are, but not using the
         * method specified in UUID specs).
         *
         * @param name tag URI to base UUID on.
         * @param hasher Hashing algorithm to use. Note that the caller has to
         *  make sure that it's thread-safe to use 'hasher', either by never
         *  calling this method from multiple threads, or by explicitly sync'ing
         *  the calls.
         */
        public UUID GenerateTagURIBasedUUID(TagURI name, MD5 hasher)
        {
            return GenerateNameBasedUUID(null, name.ToString(), hasher);
        }

    }

}