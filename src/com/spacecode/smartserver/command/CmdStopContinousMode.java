package com.spacecode.smartserver.command;

import com.spacecode.smartserver.SmartServer;
import com.spacecode.smartserver.helper.DeviceHandler;
import com.spacecode.smartserver.helper.SmartLogger;
import io.netty.channel.ChannelHandlerContext;

import java.net.SocketAddress;
import java.util.List;

/**
 * Created by MY on 23/10/2015.
 */
public class CmdStopContinousMode extends ClientCommand{
    public void execute(ChannelHandlerContext ctx, String[] parameters) {

        if(SmartServer.getClientCount() > 1){
            List<String> lst = SmartServer.getClientList();
            SmartLogger.getLogger().info("Other client still on");
            SmartServer.sendMessage(ctx, new String[]{"stopcontinousscan", "true"});
        }
        else {
            DeviceHandler.isContinousMode = false;
            boolean result = DeviceHandler.getDevice().stopScan();
            SmartServer.sendMessage(ctx, new String[]{"stopcontinousscan", result ? "true" : "false"});
        }
    }
}
