package com.spacecode.smartserver.command;

import com.spacecode.smartserver.SmartServer;
import com.spacecode.smartserver.command.ClientCommand;
import com.spacecode.smartserver.command.CommandContract;
import com.spacecode.smartserver.helper.DeviceHandler;
import io.netty.channel.ChannelHandlerContext;

@CommandContract(
   deviceRequired = true,
   responseIfInvalid = "ERROR"
)
public class CmdDeviceStatus extends ClientCommand {

   public void execute(ChannelHandlerContext ctx, String[] parameters) {
      SmartServer.sendMessage(ctx, new String[]{"devicestatus", DeviceHandler.getDeviceStatus().name()});
   }
}
