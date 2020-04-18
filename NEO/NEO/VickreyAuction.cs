using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;
using System.Text;

namespace NEO
{
    public class VickreyAuction : SmartContract
    {
        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> Transferred;

        private static readonly byte[] Owner = "AK2nJJpJr6o664CWJKi1QRXjqeic2zRp8y".ToScriptHash(); //Owner Address
        private static readonly BigInteger TotalSupplyValue = 10000000000000000;

        private static readonly byte[] neo_asset_id = { 155, 124, 255, 218, 166, 116, 190, 174, 15, 147, 14, 190, 96, 133, 175, 144, 147, 229, 254, 86, 179, 74, 92, 34, 12, 205, 207, 110, 252, 51, 111, 197 };
        private static readonly byte[] gas_asset_id = { 231, 45, 40, 105, 121, 238, 108, 177, 183, 230, 93, 253, 223, 178, 227, 132, 16, 11, 141, 20, 142, 119, 88, 222, 66, 228, 22, 139, 113, 121, 44, 96 };

        public static object Main(string method, object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return Runtime.CheckWitness(Owner);
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                var callscript = ExecutionEngine.CallingScriptHash;

                if (method == "BalanceOf") return BalanceOf((byte[])args[0]);

                if (method == "Decimals") return Decimals();

                if (method == "Deploy") return Deploy();

                if (method == "Name") return Name();

                if (method == "Symbol") return Symbol();

                if (method == "SupportedStandards") return SupportedStandards();

                if (method == "TotalSupply") return TotalSupply();

                if (method == "Transfer") return Transfer((byte[])args[0], (byte[])args[1], (BigInteger)args[2]);

                if (method == "Init") return Init((int)args[0], (int)args[1], (int)args[2], (int)args[3]);

                if (method == "Bid") return Bid((byte[])args[0], (string)args[1], (int)args[2]);

                if (method == "Claim") return Claim();

                if (method == "Reveal") return Reveal((int)args[0], (int)args[1]);

                if (method == "Withdraw") return Withdraw();
            }
            return false;
        }

        [DisplayName("balanceOf")]
        public static BigInteger BalanceOf(byte[] account)
        {
            if (account.Length != 20)
                throw new InvalidOperationException("The parameter account SHOULD be 20-byte addresses.");
            StorageMap asset = Storage.CurrentContext.CreateMap(nameof(asset));
            return asset.Get(account).AsBigInteger();
        }
        [DisplayName("decimals")]
        public static byte Decimals() => 8;

        private static bool IsPayable(byte[] to)
        {
            var c = Blockchain.GetContract(to);
            return c == null || c.IsPayable;
        }

        [DisplayName("deploy")]
        public static bool Deploy()
        {
            if (TotalSupply() != 0) return false;
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            contract.Put("totalSupply", TotalSupplyValue);
            StorageMap asset = Storage.CurrentContext.CreateMap(nameof(asset));
            asset.Put(Owner, TotalSupplyValue);
            Transferred(null, Owner, TotalSupplyValue);
            return true;
        }

        [DisplayName("name")]
        public static string Name() => "VA"; //name of the token

        [DisplayName("symbol")]
        public static string Symbol() => "VA"; //symbol of the token

        [DisplayName("supportedStandards")]
        public static string[] SupportedStandards() => new string[] { "NEP-5", "NEP-7", "NEP-10" };

        [DisplayName("totalSupply")]
        public static BigInteger TotalSupply()
        {
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            return contract.Get("totalSupply").AsBigInteger();
        }

        //VICKREY METHODS
        //I SET ALL METHODS TO VOID FOR THE MOMENT
        private static bool Init(int amount, int reservePrice, int biddingPeriod, int revealingPeriod)
        {
            if (!Runtime.CheckWitness(Owner)) return false;

            Transferred(Owner, null, amount);

            Storage.Put(Storage.CurrentContext, "amount", amount);
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
            revealed.Put(Owner, BoolToBytes(true));     //bool boolValueBack = BitConverter.ToBoolean(bytes, 0);
            return true;

        }

        private static bool Bid(byte[] account, string hash, int nonce)
        {
            StorageMap balanceOf = Storage.CurrentContext.CreateMap(nameof(balanceOf));
            StorageMap hashedBidOf = Storage.CurrentContext.CreateMap(nameof(hashedBidOf));
            StorageMap nonceOf = Storage.CurrentContext.CreateMap(nameof(nonceOf));
            nonceOf.Put(account, nonce);

            if (Runtime.CheckWitness(Owner)) return false;

            byte[] endOfBidding = Storage.Get(Storage.CurrentContext, "endOfBidding");

            if (Runtime.Time < (uint)BytesToBigInteger(endOfBidding))
            {
                hashedBidOf.Put(account, hash);
            }

            return true;
        }

        private static bool Claim()
        {
            //TODO
            byte[] endOfRevealing = Storage.Get(Storage.CurrentContext, "endOfRevealing");
            byte[] highBidder = Storage.Get(Storage.CurrentContext, "highBidder");
            byte[] amount = Storage.Get(Storage.CurrentContext, "amount");

            if (!Runtime.CheckWitness(highBidder)) return false;

            if (Runtime.Time >= (uint)BytesToBigInteger(endOfRevealing))
            {
                Transferred(null, highBidder, (int)BytesToBigInteger(amount));
            }

            return true;
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

        private static string BytesToString(byte[] data) => data.AsString();

        private static byte[] BoolToBytes(bool val) => val ? (new byte[1] { 1 }) : (new byte[1] { 0 });

        private static BigInteger BytesToBigInteger(byte[] data) => data.AsBigInteger();
    }
}
