# AuraMQTT

WORK IN PROGRESS, NOT FINISHED YET!

The goal behind this litte pogram is to receive a RGB code from a MQTT broker and sync the LEDs of Mainboard andthe connected LED Strip
and RGB Fans with this colors.

I use a raspberry with openhab2 and a MQTT broker on it. I can sync all lamps and a LED strip in my room via openhab2. I got the idea
that the colors of the Mainboard (Asus) should sync with it with one command without opening the Aura Software on my PC.

Details will follow when the program is usable, currently working on it. My code will not be the best, it's my first time to write in C#.
Fell free to use the code and modify it.
Further information will follow when the program can be used

I use the following libs: (both available from NuGet)

C# wrapper for the ASUS Aura SDK
https://github.com/nicoco007/AuraSDK/

M2MQTTClient
https://m2mqtt.wordpress.com/

