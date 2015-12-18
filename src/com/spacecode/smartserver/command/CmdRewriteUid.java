package com.spacecode.smartserver.command;

import com.spacecode.sdk.device.data.RewriteUidResult;
import com.spacecode.smartserver.SmartServer;
import com.spacecode.smartserver.command.ClientCommand;
import com.spacecode.smartserver.command.CommandContract;
import com.spacecode.smartserver.helper.DeviceHandler;
import io.netty.channel.ChannelHandlerContext;

@CommandContract(
   paramCount = 2,
   strictCount = true,
   deviceRequired = true,
   responseIfInvalid = "ERROR"
)
public class CmdRewriteUid extends ClientCommand {

   public void execute(ChannelHandlerContext ctx, String[] parameters) {
      RewriteUidResult result = DeviceHandler.getDevice().rewriteUid(parameters[0], parameters[1]);
      SmartServer.sendMessage(ctx, new String[]{"rewriteuid", result.name()});
   }
}
