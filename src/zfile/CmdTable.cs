// using System;
// using System.Collections.Generic;

// public class CmdTable
// {
//     private Dictionary<string, int> cmdList;

//     public CmdTable()
//     {
//         cmdList = new Dictionary<string, int>
//         {
//             { "cm_renmov", 906 },
//             { "cm_delete", 908 },
//             { "cm_mkdir", 907 },
//             { "cm_renameonly", 1002 },
//             { "cm_properties", 1003 },
//             { "cm_multirename", 1010 },
//         };
//     }

//     public int GetCommandId(string command)
//     {
//         if (cmdList.ContainsKey(command))
//         {
//             return cmdList[command];
//         }
//         throw new ArgumentException("Command not found");
//     }
// } 