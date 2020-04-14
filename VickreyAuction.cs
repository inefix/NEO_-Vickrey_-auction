using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System.Numerics;
using System;

namespace VickreyAuction {
    public class VickreyAuction : SmartContract {
        
        public static string Main(string method, params object[] args){
            //asset transfer
            if(Runtime.Trigger == TriggerType.Application) return "OwnerNotKnown, NEED TO HAVE IT IN PARAMS"; //return Runtime.CheckWitness(Owner);

           //no asset transfer
            if (Runtime.Trigger == TriggerType.Application) {
                switch (method) {
                    case "Initialization":
                            return Initialization((byte[])args[0], (int)args[1], (int)args[2],(int)args[3]);
                    case "Bid":
                            return Bid((string)args[0]);
                    case "Claim":
                            return Claim();
                    case "Transfer":
                            return Transfer((byte[])args[0], (byte[])args[1], (BigInteger)args[2], (byte[])args[3]);
                    case "Reveal":
                            return Reveal((int)args[0], (int)args[1]);
                    case "Withdraw":
                            return Withdraw();
                    default:
                            return method;
                }
            }
            return method;
        }
        
        //VICKREY METHODS
        //I SET ALL METHODS TO VOID FOR THE MOMENT
        private static string Initialization(/*TODO : check type*/byte[] token, int reservePrice, int biddingPeriod, int revealingPeriod){
            //TODO: OWNER MUST BE IN PARAMS
            //if (!Runtime.CheckWitness(owner)) return false;
            Storage.Put(Storage.CurrentContext, token, token);
            
            // TODO
            return "";

        }
    
        private static string Bid(string hash){
            //TODO
            return "";
        }
    
        private static string Claim(){
            //TODO
            return "";
        }
    
        private static string Transfer(byte[] from, byte[] to, BigInteger amount, byte[] callscript){
            //Check parameters
            //transfer the wallet address to byte array
            if (from.Length != 20 || to.Length != 20)
                    throw new InvalidOperationException("The parameters from and to SHOULD be 20-byte addresses.");
            if (amount <= 0)
                    throw new InvalidOperationException("The parameter amount MUST be greater than 0.");
            if (!Runtime.CheckWitness(from))
                    return "false";
            StorageMap asset = Storage.CurrentContext.CreateMap(nameof(asset));
            var fromAmount = asset.Get(from).AsBigInteger();
            if (fromAmount < amount)
                    return "false";
            if (from == to)
                    return "true";

            //Reduce payer balances
            if (fromAmount == amount)
                    asset.Delete(from);
            else
                    asset.Put(from, fromAmount - amount);

            //Increase the payee balance
            var toAmount = asset.Get(to).AsBigInteger();
            asset.Put(to, toAmount + amount);

            //Transferred(from, to, amount);
            //return true;
            return "True";
        }
    
        private static string Reveal(int amount, int nonce){
            //TODO
            return "";
        }
    
        private static string Withdraw(){
            //TODO
            return "";
        }
    }
}
