package com.spacecode.smartserver.command;

import com.spacecode.sdk.device.data.DeviceStatus;
import com.spacecode.smartserver.SmartServer;
import com.spacecode.smartserver.command.ClientCommand;
import com.spacecode.smartserver.command.CommandContract;
import com.spacecode.smartserver.helper.DeviceHandler;
import com.spacecode.smartserver.helper.SmartLogger;
import io.netty.channel.ChannelHandlerContext;
import java.util.ArrayList;
import java.util.Arrays;

@CommandContract(
   paramCount = 1,
   deviceRequired = true
)
public class CmdStartLighting extends ClientCommand {

   public void execute(ChannelHandlerContext ctx, String[] parameters) {
       //SmartLogger.getLogger().info("DeviceStatus Before Lighting - "+ DeviceHandler.getDeviceStatus().toString());
       if (DeviceHandler.getDeviceStatus() != DeviceStatus.READY && DeviceHandler.isInScan()) {
           DeviceHandler.getDevice().stopScan();
           SmartServer.sendMessage(ctx, new String[]{"startlighting", "false"});
       } else {
           boolean result = DeviceHandler.getDevice().startLightingTagsLed(new ArrayList(Arrays.asList(parameters)));

           SmartServer.sendMessage(ctx, new String[]{"startlighting", result ? "true" : "false"});
       }
   }
}
