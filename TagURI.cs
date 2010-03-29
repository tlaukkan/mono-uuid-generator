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

using System.Text;
using System;

namespace Jug
{

    /**
     * A class that allows creation of tagURI instances.
     *
     * TagURIs are specified in IETF draft <draft-kindberg-tag-uri-01.txt>;
     * available for example at:
     * 
     * http://sunsite.cnlab-switch.ch/ftp/mirror/internet-drafts/draft-kindberg-tag-uri-01.txt
     */
    public class TagURI
    {
        private String mDesc;

        /**
         * Constructor for creating tagURI instances.
         *
         * Typical string representations of tagURIs may look like:
         * <ul>
         * <li>tag:hp1.hp.com,2001:tst.1234567890
         * <li>tag:fred@flintstone.biz,2001-07-02:rock.123
         * </ul>
         * (see tagURI draft for more examples and full explanation of the
         * basic concepts)
         *
         * @param authority Authority that created tag URI; usually either a
         *   fully-qualified domain name ("www.w3c.org") or an email address
         *   ("tatu.saloranta@iki.fi").
         * @param identifier A locally unique identifier; often file path or
         *   URL path component (like, "tst.1234567890", "/home/tatu/index.html")
         * @param date Date to add as part of the tag URI, if any; null is used
         *   used to indicate that no datestamp should be added.
         * 
         */
        public TagURI(String authority, String identifier, Nullable<DateTime> date)
        {
            StringBuilder b = new StringBuilder();
            b.Append("tag:");
            b.Append(authority);
            if (date != null)
            {
                b.Append(',');
                b.Append(date.Value.Year);
                // Month is optional if it's "january" and day is "1st":
                int month = date.Value.Month;
                int day = date.Value.Day;
                if (month != 1 || day != 1)
                {
                    b.Append('-');
                    b.Append(month);
                }
                if (day != 1)
                {
                    b.Append('-');
                    b.Append(day);
                }
            }

            b.Append(':');
            b.Append(identifier);

            mDesc = b.ToString();
        }

        public override String ToString()
        {
            return mDesc;
        }

        public override bool Equals(Object o)
        {
            if (o != null && o.GetType() == typeof(TagURI))
            {
                return mDesc.Equals(((TagURI)o).ToString());
            }
            return false;
        }

        public override int GetHashCode()
        {
            return mDesc.GetHashCode();
        }

    }

}