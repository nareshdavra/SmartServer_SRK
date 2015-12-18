package com.spacecode.smartserver.command;

import com.spacecode.smartserver.SmartServer;
import com.spacecode.smartserver.command.ClientCommand;
import com.spacecode.smartserver.command.ClientCommandException;
import com.spacecode.smartserver.helper.DeviceHandler;
import com.spacecode.smartserver.helper.SmartLogger;
import com.spacecode.smartserver.helper.StringOPs;
import io.netty.channel.ChannelHandlerContext;

import java.util.concurrent.ExecutionException;
import java.util.logging.Level;

public class CmdDisconnect extends ClientCommand {

   public void execute(ChannelHandlerContext ctx, String[] parameters) throws ClientCommandException {
       try {
           SmartLogger.getLogger().info("Device disconnetced, connected clients :" + StringOPs.getCommaSaparetedString(SmartServer.getClientList())); //
           SmartServer.removeClientList(ctx);
           ctx.channel().close();           
       }
       catch (Exception exp )
       {

       }
   }
}
