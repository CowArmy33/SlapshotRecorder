using SlapshotRecorder;
using HarmonyLib;
using MelonLoader;
using System.IO.Compression;
using UnityEngine;
using System.Numerics;

namespace SlapshotRecorder
{
    [HarmonyPatch(typeof(Puck), "Tick")]
    public class PuckPatcher : MelonMod
    {
        public static GameObject puck;
        private static TransformData lastPosition;
        public static Game game;
        [HarmonyPostfix]
        public static void Postfix(Puck __instance, float deltaTime)
        {
            if (puck is null)
            {
                puck = __instance.gameObject;
                lastPosition = new TransformData(puck);
                game = GameObject.FindObjectOfType<Game>();
                HandyTools.logMsg("Found game and Puck");
            }

            Transform trans = puck.transform;
            if (HandyTools.checkChange(trans, lastPosition))
            {
                String printable = "PUCK " + game.GameTick + " " + HandyTools.getInformation(puck);

                HandyTools.logMsg(printable);
                HandyTools.WriteLine(printable);
                lastPosition.updateFields(trans);
            }
        }
    }
    [HarmonyPatch(typeof(Puck), "OnDestroy")]
    public class puckDestroyer : MelonMod
    {
        [HarmonyPostfix]
        public static void Postfix(Puck __instance)
        {
            PuckPatcher.puck = null;
            PuckPatcher.game = null;
        }
    }



    [HarmonyPatch(typeof(Game), "OnDestroy")]
    public class logCloser : MelonMod
    {
        public static void Postfix(Puck __instance)
        {
            if (HandyTools.closeWriter())
            {
                HandyTools.logMsg("Stopped writing out to file!");
                HandyTools.logMsg("Zipping file...");

                if (File.Exists("result.zip")) { File.Delete("results.zip"); }

                try { ZipFile.CreateFromDirectory("Recordings", "result.zip"); }
                catch
                {
                    HandyTools.logError("Something went wrong zipping! File not compressed!");
                    return;
                }
                HandyTools.logMsg("File zipped and compressed! Deleting unzipped version...");
                try { File.Delete("Recordings/log.txt"); }
                catch
                {
                    HandyTools.logError("Something went wrong deleting the uncompressed file!");
                    return;
                }
                HandyTools.logMsg("File has been deleted!");
            }
            else
            {
                HandyTools.logError("Could not close output file! May cause errors! Is something preventing the program from closing?");
            }
        }
    }
    public class HandyTools
    {
        public static int roundingNumber = 2;
        public static float minimumKeyframeDistance = .15f;
        public static float minimumKeyframeRotation = 1;
        public static MelonLogger.Instance log = null;
        public static StreamWriter output = null;
        public static void logMsg(String msg)
        {
            if (log is null)
            {
                log = new MelonLogger.Instance("ClassLibrary3");
            }
            log.Msg(msg);
        }
        public static void logError(String msg)
        {
            if (log is null)
            {
                log = new MelonLogger.Instance("ClassLibrary3");
            }
            log.Error(msg);
        }
        public static bool closeWriter()
        {
            try
            {
                HandyTools.output.Close();
                HandyTools.output = null;
            }
            catch { return false; }
            return true;
        }
        public static bool openWriter()
        {
            try
            {
                HandyTools.output = File.CreateText("Recordings/log.txt");
                HandyTools.logMsg(System.IO.Directory.GetCurrentDirectory());
            }
            catch { return false; }
            return true;
        }
        public static String getPosition(GameObject gameObject)
        {
            String retVal = "";
            retVal += Math.Round(gameObject.transform.position.x, roundingNumber) + " ";
            retVal += Math.Round(gameObject.transform.position.y, roundingNumber) + " ";
            retVal += Math.Round(gameObject.transform.position.z, roundingNumber);
            return retVal;
        }
        public static String getRotation(GameObject gameObject)
        {
            String retVal = "";
            retVal += Math.Round(gameObject.transform.rotation.eulerAngles.x, roundingNumber) + " ";
            retVal += Math.Round(gameObject.transform.rotation.eulerAngles.y, roundingNumber) + " ";
            retVal += Math.Round(gameObject.transform.rotation.eulerAngles.z, roundingNumber);
            return retVal;
        }
        public static String getInformation(GameObject gameObject)
        {
            return getPosition(gameObject) + " " + getRotation(gameObject);
        }
        public static bool checkChange(Transform current, TransformData prev)
        {
            bool truth = false;

            if (Math.Abs(current.position.x - prev.pos.x) > minimumKeyframeDistance)
            {
                truth = true;
            }
            else if (Math.Abs(current.position.y - prev.pos.y) > minimumKeyframeDistance)
            {
                truth = true;
            }
            else if (Math.Abs(current.position.z - prev.pos.z) > minimumKeyframeDistance)
            {
                truth = true;
            }
            else if (Math.Abs(current.rotation.eulerAngles.x - prev.rot.x) > minimumKeyframeRotation)
            {
                truth = true;
            }
            else if (Math.Abs(current.rotation.eulerAngles.y - prev.rot.y) > minimumKeyframeRotation)
            {
                truth = true;
            }
            else if (Math.Abs(current.rotation.eulerAngles.z - prev.rot.z) > minimumKeyframeRotation)
            {
                truth = true;
            }
            return truth;
        }

        internal static void WriteLine(string v)
        {
            if (HandyTools.output == null)
            {
                HandyTools.openWriter();
            }
            output.WriteLine(v);
        }
    }




    public class PlayerMemoriser
    {
        public static List<String> UUIDs = new List<String>();
        public static List<TransformData> lastRecoredPositon = new List<TransformData>();
        public static void logPlayer(Player player, float deltaTime, int gameTick)
        {
            int index = UUIDs.IndexOf(player.UUID);
            if (index >= 0 && HandyTools.checkChange(player.body.gameObject.transform, lastRecoredPositon[index]))
            {
                String printable = player.UUID + " " + gameTick + HandyTools.getInformation(player.body.gameObject);
                HandyTools.logMsg(printable);
                HandyTools.WriteLine(printable);
                lastRecoredPositon[index].updateFields(player.body.gameObject.transform);
            }
            else if (index >= 0) { }
            else
            {
                HandyTools.logError("Could not find Player of UUID: " + player.UUID + ", " + player.name);
            }
        }
        public static void birth(Player player, int gameTick)
        {
            //TODO: Handle logging birthing of players better
            UUIDs.Add(player.UUID);
            lastRecoredPositon.Add(new TransformData(player.body.gameObject));
            HandyTools.logMsg("Player " + player.UUID + " has been born!" + gameTick);
        }
        public static void kill(Player player, int gameTick)
        {
            int id = UUIDs.IndexOf(player.UUID);
            if (id != -1)
            {
                UUIDs.RemoveAt(id);
                lastRecoredPositon.RemoveAt(id);
                HandyTools.logMsg("Player " + player.name + " has been killed!" + gameTick);
            }
        }
    }



    [HarmonyPatch(typeof(Player), "OnSpawn")]
    public class PlayerBirther : MelonMod
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance)
        {
            Game game = GameObject.FindObjectOfType<Game>();
            if (game != null)
            {
                PlayerMemoriser.birth(__instance, game.GameTick);
            }
            else
            {
                HandyTools.logMsg("No Game Found");
            }
        }
    }



    [HarmonyPatch(typeof(Player), "OnDisable")]
    public class PlayerDestroyer : MelonMod
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance)
        {
            Game game = GameObject.FindObjectOfType<Game>();
            if (game != null)
            {
                PlayerMemoriser.kill(__instance, game.GameTick);
            }
        }
    }



    [HarmonyPatch(typeof(Player), "Tick")]
    public class PlayerTick : MelonMod
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance, float deltaTime, int gameTick)
        {
            PlayerMemoriser.logPlayer(__instance, deltaTime, gameTick);
        }
    }

    public class TransformData
    {
        public UnityEngine.Vector3 pos = UnityEngine.Vector3.zero;
        public UnityEngine.Vector3 rot = UnityEngine.Vector3.zero;
        public UnityEngine.Vector3 sca = UnityEngine.Vector3.zero;
        public TransformData(GameObject gameObject)
        {
            this.pos = gameObject.transform.position;
            this.rot = gameObject.transform.rotation.eulerAngles;
            this.sca = gameObject.transform.localScale;
        }
        public void updateFields(Transform trans)
        {
            this.pos = trans.position;
            this.rot = trans.rotation.eulerAngles;
            this.sca = trans.localScale;
        }
        public TransformData()
        {
        }
    }
}
