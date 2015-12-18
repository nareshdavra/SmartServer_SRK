package com.spacecode.smartserver.command;

import com.spacecode.smartserver.command.ClientCommand;
import com.spacecode.smartserver.command.CommandContract;
import com.spacecode.smartserver.helper.DeviceHandler;
import io.netty.channel.ChannelHandlerContext;

@CommandContract(
   deviceRequired = true,
   noResponseWhenInvalid = true
)
public class CmdStopScan extends ClientCommand {

   public void execute(ChannelHandlerContext ctx, String[] parameters) {
      DeviceHandler.getDevice().stopScan();
   }
}
