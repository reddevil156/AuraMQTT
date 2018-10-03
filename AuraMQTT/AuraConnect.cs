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

        public Color[] testColors = new Color[]
        {
            //new Color(255, 0, 0),
            //new Color(255, 127, 0),
            //new Color(255, 255, 0),
            //new Color(127, 255, 0),
            //new Color(0, 255, 0),
            //new Color(0, 255, 127),
            //new Color(0, 255, 255),
            //new Color(0, 127, 255),
            new Color(0, 0, 255),
            new Color(0, 0, 255),
            new Color(0, 0, 255),
            new Color(0, 0, 255)
          //new Color(127, 0, 255),
          //new Color(255, 0, 255),
          //new Color(255, 0, 127) */
        };

        public void ChangeColors(int r, int g, int b)
        {

            try
            {
                AuraSDK sdk = new AuraSDK("lib/AURA_SDK.dll");
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
            }
            catch (System.IO.FileNotFoundException)
            {

                System.Windows.MessageBox.Show("AURA_SDK.dll missing");
            }
        }


    }
}

