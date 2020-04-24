using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;


// Key is totalSupply and value is 100000000
//Storage.Put(Storage.CurrentContext, "totalSupply", 100000000);

//Get the totalSupply in the storageMap. The Map is used an entire container with key name "contract"
//StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
//return contract.Get("totalSupply").AsBigInteger();

//Storage.Put(Storage.CurrentContext, domain, owner);
//byte[] value = Storage.Get(Storage.CurrentContext, domain);

//Storage.Delete

// validate the address invoking
//if (!Runtime.CheckWitness(owner))
//return false;

//public static event transfer(byte[] from, byte[] to, BigInteger amount)

//StorageMap asset = Storage.CurrentContext.CreateMap(nameof(asset));
//BigInteger balance = asset.Get(sender).AsBigInteger();


namespace NEO
{

  public class Contract1 : SmartContract {

    private const string test_str = "Hello World";

    public static String Main(string method, params object[] args){
            Storage.Put("Hello", "World");

            //asset transfer (user may to send global asset such as NEO or GAS to or from this contract)
            if (Runtime.Trigger == TriggerType.Verification){
                return Runtime.CheckWitness(Owner);
            }


            //no asset transfer
            if (Runtime.Trigger == TriggerType.Application) {
                    switch (method) {
                    case "query":
                            return Query((string)args[0]);
                    case "register":
                            return Register((string)args[0], (byte[])args[1]);
                    case "delete":
                            return Delete((string)args[0]);
                    default:
                            return false;
                    }
            }



            return test_str;
    }

    private static bool Register(string domain, byte[] owner){
            // Check if the owner is the same as the one who invoke the contract
            if (!Runtime.CheckWitness(owner)) return false;
            byte[] value = Storage.Get(Storage.CurrentContext, domain);
            if (value != null) return false;
            Storage.Put(Storage.CurrentContext, domain, owner);
            return true;
    }

    private static bool Transfer(byte[] from, byte[] to, BigInteger amount, byte[] callscript){
            //Check parameters
            //transfer the wallet address to byte array
            if (from.Length != 20 || to.Length != 20)
                    throw new InvalidOperationException("The parameters from and to SHOULD be 20-byte addresses.");
            if (amount <= 0)
                    throw new InvalidOperationException("The parameter amount MUST be greater than 0.");
            if (!Runtime.CheckWitness(from))
                    return false;
            StorageMap asset = Storage.CurrentContext.CreateMap(nameof(asset));
            var fromAmount = asset.Get(from).AsBigInteger();
            if (fromAmount < amount)
                    return false;
            if (from == to)
                    return true;

            //Reduce payer balances
            if (fromAmount == amount)
                    asset.Delete(from);
            else
                    asset.Put(from, fromAmount - amount);

            //Increase the payee balance
            var toAmount = asset.Get(to).AsBigInteger();
            asset.Put(to, toAmount + amount);

            Transferred(from, to, amount);
            return true;
    }

  }

}
