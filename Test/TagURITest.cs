/* JUG Java Uuid Generator
 *
 * Copyright (c) 2003,2010 Eric Bie, Tommi S.E. Laukkanen, tommi.s.e.laukkanen@gmail.com
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
using System.Text;
using NUnit.Framework;
using Jug;

namespace Jug.Test
{

    /**
     * JUnit Test class for the org.safehaus.uuid.TagURI class.
     *
     * @author Eric Bie
     */
    [TestFixture]
    public class TagURITest : TestBase
    {
        private static String[] AUTHORITIES =
    {
        "www.w3c.org",
        "www.google.com",
        "www.fi",
        "tatu.saloranta@iki.fi"
    };

        private static String[] IDS =
    {
        "1234",
        "/home/billg/public_html/index.html",
        "6ba7b810-9dad-11d1-80b4-00c04fd430c8",
        "foobar"
    };

        /**
         * Test of ToString method, of class org.safehaus.uuid.TagURI.
         */
        [Test]
        public void testToString()
        {
            DateTime CALENDAR = DateTime.Now;

            // we'll test that a few expected constructed TagURI's create the
            // expected strings

            // first, some tests with a null calendar
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    TagURI tag_uri = new TagURI(AUTHORITIES[i], IDS[j], null);
                    String expected = "tag:" + AUTHORITIES[i] + ":" + IDS[j];
                    Assert.AreEqual(
                        expected,
                        tag_uri.ToString(), "Expected string did not match generated ToString()");
                }
            }

            // now some cases with date
            for (int i = 0; i < 4; ++i)
            {
                CALENDAR = new DateTime(2006, 6, 4);

                for (int j = 0; j < 4; ++j)
                {
                    TagURI tag_uri = new TagURI(AUTHORITIES[i], IDS[j], CALENDAR);
                    String expected = "tag:" + AUTHORITIES[i] + "," +
                        CALENDAR.Year + "-" +
                        CALENDAR.Month + "-" +
                        CALENDAR.Day + ":" + IDS[j];
                    Assert.AreEqual(
                        expected,
                        tag_uri.ToString(),
                        "Expected string did not match generated ToString()"
                        );
                }
            }

            // now some cases with date such that day is left out
            // (first of the month)
            for (int i = 0; i < 4; ++i)
            {
                CALENDAR = new DateTime(2006, 6, 1);


                for (int j = 0; j < 4; ++j)
                {
                    TagURI tag_uri = new TagURI(AUTHORITIES[i], IDS[j], CALENDAR);
                    String expected = "tag:" + AUTHORITIES[i] + "," +
                        CALENDAR.Year + "-" +
                        CALENDAR.Month + ":" + IDS[j];
                    Assert.AreEqual(
                        expected,
                        tag_uri.ToString(),
                        "Expected string did not match generated ToString()"
                        );
                }
            }

            // now some cases with date such that day and month are left out
            // (jan-1)
            for (int i = 0; i < 4; ++i)
            {
                CALENDAR = new DateTime(2006, 1, 1);

                for (int j = 0; j < 4; ++j)
                {
                    TagURI tag_uri = new TagURI(AUTHORITIES[i], IDS[j], CALENDAR);
                    String expected = "tag:" + AUTHORITIES[i] + "," +
                        CALENDAR.Year + ":" + IDS[j];
                    Assert.AreEqual(
                        expected,
                        tag_uri.ToString(),
                        "Expected string did not match generated ToString()"
                        );
                }
            }
        }



        /**
         * Test of Equals method, of class org.safehaus.uuid.TagURI.
         */
        [Test]
        public void testEquals()
        {
            // test passing null to Equals returns false
            // (as specified in the JDK docs for Object)
            TagURI x = new TagURI(AUTHORITIES[1], IDS[2], null);
            assertFalse("Equals(null) didn't return false",
                    x.Equals((Object)null));

            // test that passing an object which is not a TagURI returns false
            assertFalse("x.Equals(non_TagURI_object) didn't return false",
                        x.Equals(new Object()));

            // test a case where two TagURIs are definitly not equal
            TagURI w = new TagURI(AUTHORITIES[2], IDS[0], DateTime.Now);
            assertFalse("x == w didn't return false",
                        x == w);
            assertFalse("x.Equals(w) didn't return false",
                        x.Equals(w));

            // test refelexivity
            assertTrue("x.Equals(x) didn't return true",
                        x.Equals(x));

            // test symmetry
            TagURI y = new TagURI(AUTHORITIES[1], IDS[2], null);
            assertFalse("x == y didn't return false",
                        x == y);
            assertTrue("y.Equals(x) didn't return true",
                        y.Equals(x));
            assertTrue("x.Equals(y) didn't return true",
                        x.Equals(y));

            // now we'll test transitivity
            TagURI z = new TagURI(AUTHORITIES[1], IDS[2], null);
            assertFalse("x == y didn't return false",
                        x == y);
            assertFalse("x == y didn't return false",
                        y == z);
            assertFalse("x == y didn't return false",
                        x == z);
            assertTrue("x.Equals(y) didn't return true",
                        x.Equals(y));
            assertTrue("y.Equals(z) didn't return true",
                        y.Equals(z));
            assertTrue("x.Equals(z) didn't return true",
                        x.Equals(z));

            // test consistancy (this test is just calling Equals multiple times)
            assertFalse("x == y didn't return false",
                        x == y);
            assertTrue("x.Equals(y) didn't return true",
                        x.Equals(y));
            assertTrue("x.Equals(y) didn't return true",
                        x.Equals(y));
            assertTrue("x.Equals(y) didn't return true",
                        x.Equals(y));
        }

    }

}