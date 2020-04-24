using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace NEO
{
    public class VickreyAuction : SmartContract
    {
        private static readonly byte[] Owner = "AK2nJJpJr6o664CWJKi1QRXjqeic2zRp8y".ToScriptHash(); //Owner Address
        private static readonly BigInteger TotalSupplyValue = 100000000000;

        private static readonly byte[] neo_asset_id = { 155, 124, 255, 218, 166, 116, 190, 174, 15, 147, 14, 190, 96, 133, 175, 144, 147, 229, 254, 86, 179, 74, 92, 34, 12, 205, 207, 110, 252, 51, 111, 197 };
        private static readonly byte[] gas_asset_id = { 231, 45, 40, 105, 121, 238, 108, 177, 183, 230, 93, 253, 223, 178, 227, 132, 16, 11, 141, 20, 142, 119, 88, 222, 66, 228, 22, 139, 113, 121, 44, 96 };

        [DisplayName("name")]
        public static string Name() => "VNEO"; //name of the token

        [DisplayName("symbol")]
        public static string Symbol() => "VNEO"; //symbol of the token

        [DisplayName("decimals")]
        public static byte Decimals() => 8;

        [DisplayName("supportedStandards")]
        public static string[] SupportedStandards() => new string[] { "NEP-5", "NEP-7", "NEP-10" };

        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> Transferred;

        public static object Main(string method, object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return Runtime.CheckWitness(Owner);
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                if (method == "BalanceOf") return BalanceOf((byte[])args[0]);

                if (method == "Decimals") return Decimals();

                if (method == "Deploy") return Deploy();

                if (method == "Name") return Name();

                if (method == "Symbol") return Symbol();

                if (method == "SupportedStandards") return SupportedStandards();

                if (method == "TotalSupply") return TotalSupply();

                //if (method == "Transfer") return Transfer((byte[])args[0], (byte[])args[1], (BigInteger)args[2], ExecutionEngine.CallingScriptHash);

                if (method == "Init") return Init((string)args[0], (int)args[1], (int)args[2], (int)args[3]);

                if (method == "Bid") return Bid((byte[])args[0], (string)args[1]);

                if (method == "Claim") return Claim();

                if (method == "Result") return Result();

                if (method == "Reveal") return Reveal((byte[])args[0], (int)args[1], (int)args[2]);

                if (method == "Withdraw") return Withdraw();

                if (method == "GetReceiver") return GetReceiver();

                if (method == "GetSender") return GetSender();

                if (method == "GetTime") return GetCurrentTime();
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

        [DisplayName("totalSupply")]
        public static BigInteger TotalSupply()
        {
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            return contract.Get("totalSupply").AsBigInteger();
        }

        [DisplayName("deploy")]
        public static bool Deploy()
        {
            if (TotalSupply() != 0) return false;
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            contract.Put("totalSupply", TotalSupplyValue);
            StorageMap asset = Storage.CurrentContext.CreateMap(nameof(asset));
            asset.Put(Owner, TotalSupplyValue);
            Transferred(null, Owner, TotalSupplyValue);  //not transfer the coin to the owner since it will be sell
            return true;
        }

        [DisplayName("getTime")]
        public static BigInteger GetCurrentTime()
        {
            return (BigInteger)Runtime.Time;
        }

        //IS THE AMOUNT STILL USEFUL ?
        private static bool Init(string secret, BigInteger reservePrice, int durationBidding, int durationReveal)
        {
            if (!Runtime.CheckWitness(Owner)) return false;

            //Auction
            Auction auction = new Auction(secret, Runtime.Time, durationBidding, durationReveal, reservePrice, Owner);

            Storage.Put(Storage.CurrentContext, "auction", Serialize(auction));
            //StorageMap revealed = Storage.CurrentContext.CreateMap(nameof(revealed));
            //revealed.Put(Owner, BoolToBytes(true));
            return true;

        }

        private static bool Bid(byte[] bidderAddress, string hash)
        {
            if (Runtime.CheckWitness(Owner)) return false;

            //Deserialize auction
            Auction auction = (Auction)Deserialize(Storage.Get(Storage.CurrentContext, "auction"));
            if (Runtime.Time >= auction.endOfBidding) return false;

            //Store bidder's info
            Bidder bidder = new Bidder(bidderAddress, hash);
            auction.AddBidder(bidder);
            Storage.Put(Storage.CurrentContext, "auction", Serialize(auction));

            return true;
        }

        private static bool Reveal(byte[] senderAddress, int stake, int nonce)
        {
            //Get auction
            Auction auction = (Auction)Deserialize(Storage.Get(Storage.CurrentContext, "auction"));
            uint now = Runtime.Time;
            if (now < auction.endOfBidding || now >= auction.endOfRevealing) return false;

            //Get bidder's hash
            string hash = auction.GetBidderHash(senderAddress);
            if (hash == null) return false;

            //Compute hash and compare
            byte[] byteHash = AppendByteArrays(BitConverter.GetBytes(stake), BitConverter.GetBytes(nonce));
            string generatedHash = GenerateSHA256String(byteHash);
            if (hash != generatedHash) return false;

            //Confirm reveal of bidder
            auction.ConfirmReveal(senderAddress);

            if (stake > auction.highestBid)
            {
                auction.secondBid = auction.highestBid;
                auction.highestBid = stake;
                auction.higherBidder = senderAddress;
            }
            else if (stake > auction.secondBid)
            {
                auction.secondBid = stake;
            }
            Storage.Put(Storage.CurrentContext, "auction", Serialize(auction));

            Transferred(senderAddress, null, stake);

            return true;
        }

        private static string Result(byte[] senderAddress)
        {
            Auction auction = (Auction)Deserialize(Storage.Get(Storage.CurrentContext, "auction"));
            uint now = Runtime.Time;
            if (now < auction.endOfRevealing) return "wait";
            if (Runtime.CheckWitness(auction.higherBidder))
            {
                //create variable to know if highBidder has called result
                Transferred(auction.higherBidder, Owner, auction.secondBid);
                return auction.secret;
            } else
            {
                //Transferred(null, senderAddress, stake)
            }
            return "true";
        }

        private static bool End()
        {
            if (!Runtime.CheckWitness(Owner)) return false;

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
                return true;

            }

            return false;

        }

        private static bool Withdraw()
        {
            //check timing period
            byte[] endOfRevealing = Storage.Get(Storage.CurrentContext, "endOfRevealing");
            if (Runtime.Time < (uint)BytesToBigInteger(endOfRevealing)) return false;

            //check caller has revealed his bid
            byte[] caller = GetCaller();
            StorageMap revealed = Storage.CurrentContext.CreateMap(nameof(revealed));

            //transfer money from owner to caller
            StorageMap balanceOf = Storage.CurrentContext.CreateMap(nameof(balanceOf));
            Transfer(Owner, caller, balanceOf.Get(caller).AsBigInteger());
            return true;
        }

        private static bool Transfer(byte[] from, byte[] to, BigInteger amount, byte[] callscript)
        {
            //Check parameters
            if (from.Length != 20 || to.Length != 20)
                throw new InvalidOperationException("The parameters from and to SHOULD be 20-byte addresses.");
            if (amount <= 0)
                throw new InvalidOperationException("The parameter amount MUST be greater than 0.");
            if (!IsPayable(to))
                return false;
            if (!Runtime.CheckWitness(from) && from.AsBigInteger() != callscript.AsBigInteger())
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

        private static bool IsPayable(byte[] to)
        {
            var c = Blockchain.GetContract(to);
            return c == null || c.IsPayable;
        }

        private static string BytesToString(byte[] data) => data.AsString();

        private static byte[] BoolToBytes(bool val) => val ? (new byte[1] { 1 }) : (new byte[1] { 0 });

        private static BigInteger BytesToBigInteger(byte[] data) => data.AsBigInteger();

        private static string GenerateSHA256String(byte[] input)
        {
            SHA256 sha256 = SHA256Managed.Create();
            byte[] hash = sha256.ComputeHash(input);
            return GetStringFromHash(hash);
        }

        private static string GetStringFromHash(byte[] hash)
        {
            StringBuilder str = new StringBuilder();
            foreach (byte b in hash)
            {
                str.Append(b.ToString("X2"));
            }
            return str.ToString();
        }
        private static byte[] AppendByteArrays(byte[] array1, byte[] array2)
        {
            byte[] array3 = new byte[array1.Length + array2.Length];

            //append first array
            for (int i = 0; i < array1.Length; i++)
            {
                array3[i] = array1[i];
            }

            //append 2nd array
            for (int i = array1.Length; i < array3.Length; i++)
            {
                array3[i] = array2[i - array1.Length];
            }

            return array3;
        }
        private static bool CompareByteArrays(byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length) return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i]) return false;
            }

            return true;
        }

        [DisplayName("getReceiver")]
        public static byte[] GetReceiver()
        {
            return ExecutionEngine.ExecutingScriptHash;
        }

        [DisplayName("getCaller")]
        public static byte[] GetCaller()
        {
            return ExecutionEngine.CallingScriptHash;
        }

        [DisplayName("getSender")]
        public static byte[] GetSender()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] reference = tx.GetReferences();
            // you can choice refund or not refund
            foreach (TransactionOutput output in reference)
            {
                if (output.AssetId == neo_asset_id) return output.ScriptHash;
            }
            return new byte[1] { 0x20 };
        }

        private static byte[] Serialize(object obj)
        {
            if (obj == null) return null;

            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        private static Object Deserialize(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                ms.Write(bytes, 0, bytes.Length);
                ms.Seek(0, SeekOrigin.Begin);
                return bf.Deserialize(ms);
            }
        }
    }
}
