package wifeofxardas.com.github;

import org.neo.smartcontract.framework.Helper;
import org.neo.smartcontract.framework.services.neo.Storage;
import org.neo.smartcontract.framework.services.neo.Runtime;

import java.math.BigInteger;

public class SecondPriceAuction extends org.neo.smartcontract.framework.SmartContract {
  public static Object Main(String operation, byte[][] args) {
    if (operation.equals("openLot")) {
      if (args.length < 3) {
        return false;
      }

      return SecondPriceAuction.openLot(args[0], args[1], args[2]);
    } else if (operation.equals("closeLot")) {
      if (args.length < 2) {
        return false;
      }

      return SecondPriceAuction.closeLot(args[0], args[1]);
    } else if (operation.equals("getLots")) {
      if (args.length < 1) {
        return false;
      }

      return SecondPriceAuction.getLots(args[0]);
    } else if (operation.equals("getClosedLots")) {
      if (args.length < 1) {
        return false;
      }

      return SecondPriceAuction.getClosedLots(args[0]);
    } else if (operation.equals("placeStake")) {
      if (args.length < 3) {
        return false;
      }
      return SecondPriceAuction.placeStake(args[0], args[1], args[2]);
    } else if (operation.equals("confirmStake")) {
      if (args.length < 4) {
        return false;
      }

      return SecondPriceAuction.confirmStake(args[0], args[1], args[2], args[3]);
    } else if (operation.equals("setLotWinner")) {
      if (args.length < 2) {
        return false;
      }

      SecondPriceAuction.setLotWinner(args[0], args[1]);
      return true;
    }

    Storage.put(Storage.currentContext(), "Greeting to the World", "Hello World!");
    return Storage.get(Storage.currentContext(), "Greeting to the World");
  }

  public static String openLot(byte[] owner, byte[] name, byte[] desc) {
    if (!Runtime.checkWitness(owner)) {
      Runtime.log("Failed witness check");
      return "false";
    }

    byte[] id = SecondPriceAuction.getId(Helper.asByteArray("currentId"));
    byte[] lotId = Helper.concat(Helper.asByteArray("lots."), id);

    Storage.put(
        Storage.currentContext(), Helper.concat(lotId, Helper.asByteArray(".owner")), owner);
    Storage.put(Storage.currentContext(), Helper.concat(lotId, Helper.asByteArray(".name")), name);
    Storage.put(Storage.currentContext(), Helper.concat(lotId, Helper.asByteArray(".desc")), desc);
    Storage.put(
        Storage.currentContext(), Helper.concat(lotId, Helper.asByteArray(".state")), "open");

    SecondPriceAuction.addIdToOwner(owner, id);

    return "true";
  }

  public static void addIdToOwner(byte[] owner, byte[] id) {
    byte[] idsListId = Helper.concat(owner, Helper.asByteArray(".lots"));

    byte[] currentOwnerIds = Storage.get(Storage.currentContext(), idsListId);

    Storage.put(
        Storage.currentContext(),
        idsListId,
        Helper.concat(Helper.concat(currentOwnerIds, id), Helper.asByteArray(";")));
  }

  public static byte[] getId(byte[] key) {
    BigInteger id = Helper.asBigInteger(Storage.get(Storage.currentContext(), key));

    if (Helper.asString(Helper.asByteArray(id)) == "") {
      id = BigInteger.ONE;
    } else {
      id = id.add(BigInteger.ONE);
    }

    Storage.put(Storage.currentContext(), key, id);

    return SecondPriceAuction.stringifyInt(id);
  }

  public static byte[] stringifyInt(BigInteger value) {
    byte[] result = new byte[0];

    long reminder = Long.valueOf(String.valueOf(value));

    while (reminder > 0) {
      result = Helper.concat(Helper.asByteArray(BigInteger.valueOf(reminder % 10 + 48)), result);

      reminder /= 10;
    }

    return result;
  }

  public static String closeLot(byte[] caller, byte[] id) {
    if (!Runtime.checkWitness(caller)) {
      Runtime.log("Failed witness check");
      return "false";
    }

    byte[] lotId = Helper.concat(Helper.asByteArray("lots."), id);
    byte[] owner =
        Storage.get(Storage.currentContext(), Helper.concat(lotId, Helper.asByteArray(".owner")));
    byte[] stakes =
        Storage.get(Storage.currentContext(), Helper.concat(lotId, Helper.asByteArray(".stakes")));

    if (owner == caller && SecondPriceAuction.getLotState(id).equals("open")) {
      if (!Helper.asString(stakes).equals("")) {
        SecondPriceAuction.changeLotState(lotId, Helper.asByteArray("wait"));
      } else {
        SecondPriceAuction.changeLotState(lotId, Helper.asByteArray("canceled"));
        SecondPriceAuction.deleteLotFromOwner(owner, id);
      }
    } else {
      return "false";
    }

    return "true";
  }

  public static void setLotWinner(byte[] caller, byte[] id) {
    if (!Runtime.checkWitness(caller) || !SecondPriceAuction.getLotState(id).equals("wait")) {
      Runtime.log("can not set winner");
      return;
    }

    byte[] lotId = Helper.concat(Helper.asByteArray("lots."), id);
    byte[] owner =
        Storage.get(Storage.currentContext(), Helper.concat(lotId, Helper.asByteArray(".owner")));

    if (owner != caller) {
      Runtime.log("unable to finish not owned lot");
      return;
    }

    BigInteger maxPlacerId =
        Helper.asBigInteger(
            Storage.get(
                Storage.currentContext(), Helper.concat(lotId, Helper.asByteArray(".stakes"))));

    byte[] maxStake = Helper.asByteArray(BigInteger.ZERO);
    byte[] secondMax = Helper.asByteArray(BigInteger.ZERO);
    byte[] winner = {};

    while (maxPlacerId.compareTo(BigInteger.ZERO) > 0) {
      byte[] placerAddress =
          Storage.get(
              Storage.currentContext(),
              Helper.concat(
                  lotId,
                  Helper.concat(
                      Helper.asByteArray(".stakes."),
                      SecondPriceAuction.stringifyInt(maxPlacerId))));

      byte[] placerStake =
          Storage.get(
              Storage.currentContext(),
              Helper.concat(placerAddress, Helper.concat(Helper.asByteArray(".stakes."), id)));

      if (Helper.asBigInteger(maxStake).compareTo(Helper.asBigInteger(placerStake)) <= 0
          && !Helper.asString(placerStake).equals("")) {
        maxStake = placerStake;
        winner = placerAddress;
      }

      if ((Helper.asBigInteger(secondMax).compareTo(Helper.asBigInteger(placerStake)) < 0)
          && (Helper.asBigInteger(maxStake).compareTo(Helper.asBigInteger(placerStake)) > 0)) {
        secondMax = placerStake;
      }

      maxPlacerId = maxPlacerId.subtract(BigInteger.ONE);
    }

    // if we have only one stake
    if (Helper.asString(secondMax).equals("")) {
      secondMax = maxStake;
    }

    secondMax = Helper.asByteArray(Helper.asBigInteger(secondMax).add(BigInteger.ONE));

    Storage.put(
        Storage.currentContext(), Helper.concat(lotId, Helper.asByteArray(".price")), secondMax);

    Storage.put(
        Storage.currentContext(), Helper.concat(lotId, Helper.asByteArray(".winner")), winner);

    SecondPriceAuction.changeLotState(lotId, Helper.asByteArray("closed"));
    SecondPriceAuction.deleteLotFromOwner(owner, id);
  }

  public static String deleteLotFromOwner(byte[] owner, byte[] id) {
    byte[] idsListId = Helper.concat(owner, Helper.asByteArray(".lots"));
    byte[] currentOwnerIds = Storage.get(Storage.currentContext(), idsListId);
    int index = SecondPriceAuction.indexOf(currentOwnerIds, id);

    Storage.put(
        Storage.currentContext(),
        idsListId,
        Helper.concat(
            Helper.range(currentOwnerIds, 0, index),
            Helper.range(
                // id.length + 1 bcs we have ';'
                currentOwnerIds, index + id.length + 1, currentOwnerIds.length)));

    SecondPriceAuction.toClosedLots(owner, id);

    return "true";
  }

  public static String toClosedLots(byte[] owner, byte[] id) {
    byte[] idsListId = Helper.concat(owner, Helper.asByteArray(".closedLots"));

    byte[] currentOwnerIds = Storage.get(Storage.currentContext(), idsListId);

    Storage.put(
        Storage.currentContext(),
        idsListId,
        Helper.concat(Helper.concat(currentOwnerIds, id), Helper.asByteArray(";")));

    return "true";
  }

  public static void changeLotState(byte[] lotId, byte[] state) {
    Storage.put(
        Storage.currentContext(), Helper.concat(lotId, Helper.asByteArray(".state")), state);
  }

  public static String getLots(byte[] owner) {
    return Helper.asString(
        Storage.get(Storage.currentContext(), Helper.concat(owner, Helper.asByteArray(".lots"))));
  }

  public static String getClosedLots(byte[] owner) {
    return Helper.asString(
        Storage.get(
            Storage.currentContext(), Helper.concat(owner, Helper.asByteArray(".closedLots"))));
  }

  /**
   * Place stake.
   *
   * <p>Add stake hash to [placerAdd].stakes.[lotId] storage key
   *
   * @param placer placer address
   * @param id lot id
   * @param stakeHash hash of stake value
   * @return true\false
   */
  public static Object placeStake(byte[] placer, byte[] id, byte[] stakeHash) {
    if (!Runtime.checkWitness(placer)) {
      Runtime.log("you should not pass!!!");
      return "false";
    }
    if (!SecondPriceAuction.getLotState(id).equals("open")) {
      Runtime.log("can not find lot or it is not in wait state");
      return "false";
    }

    String currentPlacerStake = SecondPriceAuction.getHashedStake(placer, id);

    if (!currentPlacerStake.equals("")) {
      Runtime.log("Can not change stake hash");
      return "false";
    }

    byte[] lotId = Helper.concat(Helper.asByteArray("lots."), id);
    byte[] placersId =
        SecondPriceAuction.getId(Helper.concat(lotId, Helper.asByteArray(".stakes")));

    Storage.put(
        Storage.currentContext(),
        Helper.concat(lotId, Helper.concat(Helper.asByteArray(".stakes."), placersId)),
        placer);

    SecondPriceAuction.addStakeHashToUser(placer, id, stakeHash);

    return "true";
  }

  public static String confirmStake(byte[] placer, byte[] id, byte[] stake, byte[] stakeSalt) {
    if (!Runtime.checkWitness(placer)) {
      Runtime.log("you should not pass!!!");
      return "false";
    }
    if (!SecondPriceAuction.getLotState(id).equals("wait")) {
      Runtime.log("Can not find lot or it canceled");
      return "false";
    }

    String currentPlacerStakeHash = SecondPriceAuction.getHashedStake(placer, id);

    Runtime.log(Helper.asString(SecondPriceAuction.sha256(Helper.concat(stake, stakeSalt))));

    if (!currentPlacerStakeHash.equals(
        Helper.asString(SecondPriceAuction.sha256(Helper.concat(stake, stakeSalt))))) {
      Runtime.log("Hash does not equal stake + salt");
      return "false";
    }

    SecondPriceAuction.addStakeToUser(placer, id, stake);

    return "true";
  }

  public static String getLotState(byte[] id) {
    return Helper.asString(
        Storage.get(
            Storage.currentContext(),
            Helper.concat(
                Helper.concat(Helper.asByteArray("lots."), id), Helper.asByteArray(".state"))));
  }

  public static String getHashedStake(byte[] placer, byte[] lotId) {
    return Helper.asString(
        Storage.get(
            Storage.currentContext(),
            Helper.concat(placer, Helper.concat(Helper.asByteArray(".stakes.hashed"), lotId))));
  }

  /**
   * add stake's hash to user
   *
   * @param userAddress neo address
   * @param lotId lot id
   * @param stakeHash stake hash
   */
  public static void addStakeHashToUser(byte[] userAddress, byte[] lotId, byte[] stakeHash) {
    Storage.put(
        Storage.currentContext(),
        Helper.concat(userAddress, Helper.concat(Helper.asByteArray(".stakes.hashed"), lotId)),
        stakeHash);
  }

  public static void addStakeToUser(byte[] userAddress, byte[] lotId, byte[] stake) {
    Storage.put(
        Storage.currentContext(),
        Helper.concat(userAddress, Helper.concat(Helper.asByteArray(".stakes."), lotId)),
        stake);
  }

  public static Object confirmPay() {
    return "a";
  }

  public static int indexOf(byte[] where, byte[] what) {
    int resultIndex = -1;

    if (where.length < what.length) {
      return resultIndex;
    }

    for (int currentIndex = 0; currentIndex < where.length; currentIndex++) {
      if (where[currentIndex] == what[0]) {
        resultIndex = currentIndex;

        for (int searchIndex = 0; searchIndex < what.length; searchIndex++) {
          if (what[searchIndex] != where[currentIndex + searchIndex]) {
            resultIndex = -1;
            break;
          }
        }

        if (resultIndex != -1) {
          break;
        }
      }
    }

    return resultIndex;
  }
}