package com.spacecode.smartserver.command;

import com.spacecode.smartserver.SmartServer;
import com.spacecode.smartserver.command.ClientCommand;
import com.spacecode.smartserver.command.CommandContract;
import com.spacecode.smartserver.helper.DeviceHandler;
import io.netty.channel.ChannelHandlerContext;

@CommandContract(
   deviceRequired = true
)
public class CmdStopLighting extends ClientCommand {

   public void execute(ChannelHandlerContext ctx, String[] parameters) {

       boolean result = DeviceHandler.getDevice().stopLightingTagsLed();
       DeviceHandler.setLighting(false);
       SmartServer.sendMessage(ctx, new String[]{"stoplighting", result ? "true" : "false"});
        if(DeviceHandler.isContinousMode == true)
        {
            if(DeviceHandler.IsThereMoreFoundTags())
            {
                DeviceHandler.lightFoundTags();
            }
            else {
                DeviceHandler.getDevice().requestScan();
            }
        }
   }
}
