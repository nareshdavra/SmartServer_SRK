package com.spacecode.smartserver.command;

import com.spacecode.smartserver.SmartServer;
import com.spacecode.smartserver.command.ClientCommand;
import com.spacecode.smartserver.command.CommandContract;
import com.spacecode.smartserver.helper.DeviceHandler;
import io.netty.channel.ChannelHandlerContext;

@CommandContract(
   deviceRequired = true,
   noResponseWhenInvalid = true
)
public class CmdInitialization extends ClientCommand {

   public void execute(ChannelHandlerContext ctx, String[] parameters) {
       SmartServer.addClientList(ctx);
       SmartServer.sendMessage(ctx, new String[]{"initialization", DeviceHandler.getDevice().getSerialNumber(), DeviceHandler.getDevice().getDeviceType().name(), DeviceHandler.getDevice().getHardwareVersion(), DeviceHandler.getDevice().getSoftwareVersion(), DeviceHandler.getDeviceStatus().name()});
       //SmartServer.sendMessage(ctx,new String[]{""})
       SmartServer.sendMessage(ctx,new String[]{"event_connected", DeviceHandler.isContinousMode ? "true":"false"});
   }
}
