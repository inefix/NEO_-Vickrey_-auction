using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;

namespace Helloworld
{
    public class Contract1 : SmartContract
    {
        private const string test_str = "Hello World";
        public static String Main(string operation, object[] args)
        {
            Storage.Put("Hello", "World");
            return test_str;
        }
    }
}
// do not use array at deploy


using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;

namespace Helloworld
{
    public class Contract1 : SmartContract
    {
        private const string test_str = "Hello World";
        public static String Main(object[] args)
        {
            Storage.Put("Hello", "World");
            return (string)args[0];
        }
    }
}
// use array at deploy
