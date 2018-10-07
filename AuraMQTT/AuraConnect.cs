using AuraSDKDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuraMQTT
{
    class AuraConnect
    {

        public AuraConnect()
        {
            
        }

        /*
         * Return:
         * -1 if AURA_SDK.dll is missing
         * 0 if no Motherboards found
         * 1 if Motherboards found
         * 
         * 
         */

        public int ChangeColors(int r, int g, int b)
        {

            try
            {
                AuraSDK sdk = new AuraSDK(@"lib/AURA_SDK.dll");
                if (sdk.Motherboards.Length == 0)
                {
                    return 0;
                }

                foreach (Motherboard motherboard in sdk.Motherboards)
                {
                    motherboard.SetMode(DeviceMode.Software);

                    Color[] colors = new Color[motherboard.LedCount];

                    for (int i = 0; i < colors.Length; i++)
                    {
                        colors[i] = new Color((byte)r, (byte)g, (byte)b);
                    }

                    motherboard.SetColors(colors);
                }
                sdk.Unload();
                return 1;
                
            }
            catch (System.IO.FileNotFoundException)
            {

                System.Windows.MessageBox.Show("AURA_SDK.dll missing");
                return -1;
            }
        }


    }
}

