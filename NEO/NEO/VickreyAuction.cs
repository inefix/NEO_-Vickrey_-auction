using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;


namespace NEO
{
    public class VickreyAuction : SmartContract
    {

        //Token Settings
        public static string Name() => "Test";
        public static string Symbol() => "TT";
        public static readonly byte[] Owner = "AK2nJJpJr6o664CWJKi1QRXjqeic2zRp8y".ToScriptHash();
        public static byte Decimals() => 8;
        private const ulong factor = 100000000; //decided by Decimals()
        private const ulong neo_decimals = 100000000;

        private static readonly byte[] neo_asset_id = { 155, 124, 255, 218, 166, 116, 190, 174, 15, 147, 14, 190, 96, 133, 175, 144, 147, 229, 254, 86, 179, 74, 92, 34, 12, 205, 207, 110, 252, 51, 111, 197 };
        private static readonly byte[] gas_asset_id = { 231, 45, 40, 105, 121, 238, 108, 177, 183, 230, 93, 253, 223, 178, 227, 132, 16, 11, 141, 20, 142, 119, 88, 222, 66, 228, 22, 139, 113, 121, 44, 96 };

        private const ulong token_amount = 30000000 * factor; // pre ico token amount

        private const int start_time = 1506787200;
        private const int end_time = 1538323200;

        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> Transferred;

        [DisplayName("refund")]
        public static event Action<byte[], BigInteger> Refund;


        public static Object Main(string method, params object[] args)
        {
            //asset transfer
            if (Runtime.Trigger == TriggerType.Verification)
            {
                if (Owner.Length == 20)
                {
                    // if param Owner is script hash
                    return Runtime.CheckWitness(Owner);
                }
                else if (Owner.Length == 33)
                {
                    // if param Owner is public key
                    byte[] signature = method.AsByteArray();
                    return VerifySignature(signature, Owner);
                }
            }

            //no asset transfer
            if (Runtime.Trigger == TriggerType.Application)
            {
                switch (method)
                {
                    case "Deploy":
                        return Deploy((byte[])args[0], (int)args[1], (int)args[2], (int)args[3]);
                    case "Bid":
                        return Bid((string)args[0], (int)args[1]);
                    case "Claim":
                        return Claim();
                    case "Transfer":
                        return Transfer((byte[])args[0], (byte[])args[1], (BigInteger)args[2]);
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
        private static bool Deploy(/*TODO : check type*/byte[] token, int reservePrice, int biddingPeriod, int revealingPeriod)
        {
            if (!Runtime.CheckWitness(Owner)) return false;
            Storage.Put(Storage.CurrentContext, Owner, token_amount);
            Storage.Put(Storage.CurrentContext, "totalSupply", token_amount);
            Storage.Put(Storage.CurrentContext, "reservePrice", reservePrice);
            uint now = Runtime.Time;
            int endOfBidding = (int)now + biddingPeriod;
            int endOfRevealing = (int)endOfBidding + revealingPeriod;
            Storage.Put(Storage.CurrentContext, "biddingPeriod", biddingPeriod);
            Storage.Put(Storage.CurrentContext, "revealingPeriod", revealingPeriod);
            Storage.Put(Storage.CurrentContext, "now", now);
            Storage.Put(Storage.CurrentContext, "endOfBidding", endOfBidding);
            Storage.Put(Storage.CurrentContext, "endOfRevealing", endOfRevealing);
            byte[] highBidder = Owner;
            int highBid = reservePrice;
            int secondBid = reservePrice;
            Storage.Put(Storage.CurrentContext, "highBidder", highBidder);
            Storage.Put(Storage.CurrentContext, "highBid", highBid);
            Storage.Put(Storage.CurrentContext, "secondBid", secondBid);
            StorageMap revealed = Storage.CurrentContext.CreateMap(nameof(revealed));
            revealed.Put(Owner, "true");     //bool boolValueBack = BitConverter.ToBoolean(bytes, 0);
            Transferred(null, Owner, token_amount);
            return true;

        }

        private static string Bid(string hash, int nonce)
        {
            //TODO
            return "";
        }

        private static string Claim()
        {
            //TODO
            return "";
        }

        public static bool Transfer(byte[] from, byte[] to, BigInteger value)
        {
            if (value <= 0) return false;
            if (!Runtime.CheckWitness(from)) return false;
            if (to.Length != 20) return false;

            BigInteger from_value = Storage.Get(Storage.CurrentContext, from).AsBigInteger();
            if (from_value < value) return false;
            if (from == to) return true;
            if (from_value == value)
                Storage.Delete(Storage.CurrentContext, from);
            else
                Storage.Put(Storage.CurrentContext, from, from_value - value);
            BigInteger to_value = Storage.Get(Storage.CurrentContext, to).AsBigInteger();
            Storage.Put(Storage.CurrentContext, to, to_value + value);
            Transferred(from, to, value);
            return true;
        }

        private static string Reveal(int amount, int nonce)
        {
            //TODO
            return "";
        }

        private static string Withdraw()
        {
            //TODO
            return "";
        }
    }
}
