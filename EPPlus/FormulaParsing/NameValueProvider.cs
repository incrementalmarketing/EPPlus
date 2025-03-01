﻿/* Copyright (C) 2011  Jan Källman
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the GNU Lesser General Public License for more details.
 *
 * The GNU Lesser General Public License can be viewed at http://www.opensource.org/licenses/lgpl-license.php
 * If you unfamiliar with this license or have questions about it, here is an http://www.gnu.org/licenses/gpl-faq.html
 *
 * All code and executables are provided "as is" with no warranty either express or implied.
 * The author accepts no liability for any damage or loss of business that this product may cause.
 *
 * Code change notes:
 *
 * Author Change                      Date
 *******************************************************************************
 * Mats Alm Added		                2016-12-27
 *******************************************************************************/

namespace OfficeOpenXml.FormulaParsing
{
    public class NameValueProvider : INameValueProvider
    {
        private NameValueProvider()
        {
        }

        public static INameValueProvider Empty => new NameValueProvider();

        public bool IsNamedValue(string key, string worksheet)
        {
            return false;
        }

        public object GetNamedValue(string key)
        {
            return null;
        }

        public void Reload()
        {
        }
    }
}