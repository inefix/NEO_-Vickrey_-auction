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


namespace VickreyAuction {

  public class Contract1 : SmartContract {


    public static String Main(string method, params object[] args){

            //asset transfer (user may to send global asset such as NEO or GAS to or from this contract)
            if (Runtime.Trigger == TriggerType.Verification){
                return Runtime.CheckWitness(Owner);
            }


            //no asset transfer
            if (Runtime.Trigger == TriggerType.Application) {
                    switch (method) {
                    case "Initialization":
                            return Initialization(args[0], args[1], args[2], args[3]);
                    case "Bid":
                            return Bid();
                    case "Claim":
                            return Claim();
                    case "Transfer":
                            return Transfer(args[0], args[1], args[2], args[3]);
                    case "Reveal":
                            return Reveal(args[0], args[1]);
                    case "Withdraw":
                            return Withdraw();
                    default:
                            return false;
                    }
            }



            return test_str;
    }

    private static Initialization(/*type*/ token, int reservePrice, int biddingPeriod, int revealingPeriod){
      if (!Runtime.CheckWitness(owner)) return false;

      Storage.Put(Storage.CurrentContext, token, token);

      // TODO

    }

    private static Bid(string hash){
      //TODO
    }


    private static Claim(){
      //TODO
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

    private static Reveal(int amount, int nonce){
      //TODO
    }

    private static Withdraw(){
      //TODO
    }

  }

}
