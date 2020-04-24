using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Collections;

[Serializable()]
public class Auction
{
    public string secret{ get; set; }
    public uint startTime { get; set; }
    public uint endOfBidding { get; set; }
    public uint endOfRevealing { get; set; }

    public BigInteger reservePrice { get; set; }
    public BigInteger highestBid { get; set; }
    public BigInteger secondBid { get; set; }
    public byte[] higherBidder { get; set; }

    public List<Bidder> bidders;

    public Auction(string secret, uint startTime, int durationBidding, int durationRevealing, BigInteger reservePrice, byte[] higherBidder)
    {
        this.secret = secret;
        this.startTime = startTime;
        this.endOfBidding = startTime + (uint)durationBidding;
        this.endOfRevealing = startTime + (uint)durationBidding + (uint)durationRevealing;

        this.reservePrice = reservePrice;
        this.highestBid = reservePrice;
        this.secondBid = reservePrice;

        this.higherBidder = higherBidder;
        bidders = new List<Bidder>();
        AddBidder(new Bidder(higherBidder, "HASH")); //SEE IF NEED TO ADD OWNER AS BIDDER
    }

    public Auction()
    {
        reservePrice = 100;
        bidders = new List<Bidder>();
    }

    public void ConfirmReveal(byte[] address)
    {
        bidders[GetBidderIndex(address)].hasRevealed = true;
    }
    public void AddBidder(Bidder bidder)
    {
        int index = GetBidderIndex(bidder.address);
        if (index == -1)
        {
            bidders.Add(bidder);
        }
        else
        {
            UpdateBidderHash(index, bidder.hash);
        }
    }

    private void UpdateBidderHash(int index, string hash)
    {
        bidders[index].hash = hash;
    }

    private int GetBidderIndex(byte[] address)
    {
        for(int i = 0; i<bidders.Count; i++)
        {
            if (CompareByteArrays(bidders[i].address, address)) return i;
        }

        return -1;
    }

    public string GetBidderHash(byte[] address)
    {
        for (int i = 0; i < bidders.Count; i++)
        {
            if (CompareByteArrays(bidders[i].address, address)) return bidders[i].hash;
        }

        return null;
    }

    private bool CompareByteArrays(byte[] array1, byte[] array2)
    {
        if (array1.Length != array2.Length) return false;

        for (int i = 0; i < array1.Length; i++)
        {
            if (array1[i] != array2[i]) return false;
        }

        return true;
    }
}
