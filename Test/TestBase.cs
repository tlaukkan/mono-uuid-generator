using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace Jug.Test
{
    public class TestBase
    {
        protected void assertEquals(string message, object expected, object actual)
        {
            Assert.AreEqual(expected, actual, message);
        }

        protected void assertTrue(string message, bool condition)
        {
            Assert.IsTrue(condition, message);
        }

        protected void assertFalse(string message, bool condition)
        {
            Assert.IsFalse(condition, message);
        }

        protected void fail(string message)
        {
            Assert.Fail(message);
        }

        protected void assertEquals(string message, byte[] array1, byte[] array2)
        {
            List<int> diffIndexes = TestUtil.FindFirstDifference(array1, array2);
            string array1String = TestUtil.RenderByteArray(array1, diffIndexes, 1000);
            string array2String = TestUtil.RenderByteArray(array2, diffIndexes, 1000);
            Assert.IsTrue(diffIndexes.Count == 0, message + ": " + array1String + "!=" + array2String);
        }

        protected void assertNotEquals(string message, byte[] array1, byte[] array2)
        {
            List<int> diffIndexes = TestUtil.FindFirstDifference(array1, array2);
            string array1String = TestUtil.RenderByteArray(array1, diffIndexes, 1000);
            string array2String = TestUtil.RenderByteArray(array2, diffIndexes, 1000);
            Assert.IsFalse(diffIndexes.Count == 0, message + ": " + array1String + "!=" + array2String);
        }

        protected void fill(byte[] array, int startIndex, int count, byte value)
        {
            for (int i = startIndex; i < startIndex + count; i++)
            {
                array[i] = value;
            }
        }
    }
}
