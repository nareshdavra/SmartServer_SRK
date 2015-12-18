package com.spacecode.smartserver.command;

import com.spacecode.sdk.device.data.Inventory;
import com.spacecode.smartserver.SmartServer;
import com.spacecode.smartserver.command.ClientCommand;
import com.spacecode.smartserver.command.CommandContract;
import com.spacecode.smartserver.helper.DeviceHandler;
import io.netty.channel.ChannelHandlerContext;

@CommandContract(
   deviceRequired = true,
   responseIfInvalid = ""
)
public class CmdLastInventory extends ClientCommand {

   public void execute(ChannelHandlerContext ctx, String[] parameters) {
      this.sendInventory(ctx, DeviceHandler.getDevice().getLastInventory());
   }

   private void sendInventory(ChannelHandlerContext ctx, Inventory inventory) {
      SmartServer.sendMessage(ctx, new String[]{"lastinventory", inventory == null?"":inventory.serialize()});
   }
}
