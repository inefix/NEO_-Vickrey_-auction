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


////////////////////////////////// BID /////////////////////////////////////////
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

namespace Helloworld
{
public class Contract1 : SmartContract
{
[DisplayName("refund")]
public static event Action<byte[], BigInteger> Refund;

private const string test_str = "Hello World";
private static readonly byte[] neo_asset_id = { 155, 124, 255, 218, 166, 116, 190, 174, 15, 147, 14, 190, 96, 133, 175, 144, 147, 229, 254, 86, 179, 74, 92, 34, 12, 205, 207, 110, 252, 51, 111, 197 };
public static Object Main(string method)
{
        Storage.Put("Hello", "World");
        if (method == "Bid") return Bid();
        if (method == "Ref") return Ref();
        return false;
}

private static int Bid()
{

        int value = 0;

        //store the amount of neo sent
        Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
        TransactionOutput reference = tx.GetReferences()[0];
        if (reference.AssetId != neo_asset_id) return 0;                  //accept NEO
        //if (reference.AssetId != gas_asset_id) return 0;        //accept GAS
        byte[] sender = reference.ScriptHash;
        byte[] receiver = ExecutionEngine.ExecutingScriptHash;

        TransactionOutput[] outputs = tx.GetOutputs();
        // get the total amount of Neo
        foreach (TransactionOutput output in outputs)
        {
                if (output.ScriptHash == receiver)
                {
                        value += (int)output.Value;
                }
        }

        Storage.Put(Storage.CurrentContext, "value", value);
        return value;
}

private static bool Ref()
{
        byte[] sender = GetSender();
        ulong contribute_value = GetContributeValue();
        if (contribute_value > 0 && sender.Length != 0)
        {
                Refund(sender, contribute_value);
        }
        return true;
}

private static BigInteger BytesToBigInteger(byte[] data) => data.AsBigInteger();

private static byte[] GetSender()
{
        Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
        TransactionOutput[] reference = tx.GetReferences();
        // you can choice refund or not refund
        foreach (TransactionOutput output in reference)
        {
                if (output.AssetId == neo_asset_id) return output.ScriptHash;
        }
        return new byte[] {};
}

private static ulong GetContributeValue()
{
        Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
        TransactionOutput[] outputs = tx.GetOutputs();
        ulong value = 0;
        // get the total amount of Neo
        // 获取转入智能合约地址的Neo总量
        foreach (TransactionOutput output in outputs)
        {
                if (output.ScriptHash == GetReceiver() && output.AssetId == neo_asset_id)
                {
                        value += (ulong)output.Value;
                }
        }
        return value;
}

// get smart contract script hash
private static byte[] GetReceiver()
{
        return ExecutionEngine.ExecutingScriptHash;
}
}
}





using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;

namespace Helloworld
{
    public class Contract1 : SmartContract
    {
        private static readonly byte[] Owner = "AK2nJJpJr6o664CWJKi1QRXjqeic2zRp8y".ToScriptHash(); //Owner Address

        public static Object Main(string operation, params object[] args)
        {
                if (Runtime.Trigger == TriggerType.Verification)
                {
                        return Runtime.CheckWitness(Owner);
                }
                if (Runtime.Trigger == TriggerType.Application)
                {
                        return "Hello";
                }
                return "error";
        }
    }
}
