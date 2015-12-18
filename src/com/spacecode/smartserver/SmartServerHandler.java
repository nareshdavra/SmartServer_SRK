//
// Source code recreated from a .class file by IntelliJ IDEA
// (powered by Fernflower decompiler)
//

package com.spacecode.smartserver;

import com.spacecode.smartserver.SmartServer;
import com.spacecode.smartserver.command.ClientCommandException;
import com.spacecode.smartserver.command.ClientCommandRegister;
import com.spacecode.smartserver.helper.DeviceHandler;
import com.spacecode.smartserver.helper.SmartLogger;
import io.netty.channel.ChannelHandlerContext;
import io.netty.channel.SimpleChannelInboundHandler;
import io.netty.channel.ChannelHandler.Sharable;
import java.util.logging.Level;

@Sharable
final class SmartServerHandler extends SimpleChannelInboundHandler<String> {
    private static final ClientCommandRegister COMMAND_REGISTER = new ClientCommandRegister();

    SmartServerHandler() {
    }

    public void channelActive(ChannelHandlerContext ctx) {
        SmartServer.addClientChannel(ctx.channel(), ctx.handler());
        SmartLogger.getLogger().info("Connection from " + ctx.channel().remoteAddress());

        //SmartServer.sendMessage(ctx,new String[]{"event_connected", DeviceHandler.isContinousMode ? "true":"false"});
    }

    protected void channelRead0(ChannelHandlerContext ctx, String msg) {
        this.handleTextRequest(ctx, msg);
    }

    private void handleTextRequest(ChannelHandlerContext ctx, String request) {
        if(!request.trim().isEmpty()) {
            String[] parameters = request.split(Character.toString('\u001c'));
            SmartLogger.getLogger().info(ctx.channel().remoteAddress().toString() + " - " + parameters[0]);

            try {
                COMMAND_REGISTER.execute(ctx, parameters);
            } catch (ClientCommandException var5) {
                SmartLogger.getLogger().log(Level.SEVERE, "ClientCommand exception occurred.", var5);
            }
        }
    }

    public void exceptionCaught(ChannelHandlerContext ctx, Throwable cause) {
        SmartServer.removeClientList(ctx);
        SmartLogger.getLogger().log(Level.WARNING, "Exception caught by handler", cause);
        ctx.close();
    }
}
