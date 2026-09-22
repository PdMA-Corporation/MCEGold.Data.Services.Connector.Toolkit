/* Purpose: This is a simple application that acts as an ISBM publication Consumer.
 *          It demonstrates the idea of using an ISBM Client Adapter to read publication 
 *          from an ISBM Server Adapter. It should be interoperable with any ISBM compatible
 *          adapters regardless of the actual service bus that delivers the messages.  
 *          
 * Author: Claire Wong
 * Date Created:  2020/08/15
 * 
 * (c) 2022
 * This code is licensed under MIT license
 * 
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MCEGold.Consumer.Request
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FormMain());
        }
    }
}
