//
// Source code recreated from a .class file by IntelliJ IDEA
// (powered by Fernflower decompiler)
//

package com.spacecode.smartserver;

import com.spacecode.sdk.device.Device;
import com.spacecode.sdk.network.communication.MessageHandler;
import com.spacecode.smartserver.SmartServerHandler;
import com.spacecode.smartserver.WebSocketHandler;
import com.spacecode.smartserver.helper.ConfManager;
import com.spacecode.smartserver.helper.DeviceHandler;
import com.spacecode.smartserver.helper.SmartLogger;
import io.netty.bootstrap.ServerBootstrap;
import io.netty.buffer.Unpooled;
import io.netty.channel.Channel;
import io.netty.channel.ChannelFuture;
import io.netty.channel.ChannelHandler;
import io.netty.channel.ChannelHandlerContext;
import io.netty.channel.ChannelInitializer;
import io.netty.channel.ChannelOption;
import io.netty.channel.EventLoopGroup;
import io.netty.channel.group.ChannelGroup;
import io.netty.channel.group.ChannelGroupFuture;
import io.netty.channel.group.DefaultChannelGroup;
import io.netty.channel.nio.NioEventLoopGroup;
import io.netty.channel.socket.nio.NioServerSocketChannel;
import io.netty.handler.codec.DelimiterBasedFrameDecoder;
import io.netty.handler.codec.http.websocketx.TextWebSocketFrame;
import io.netty.handler.codec.string.StringDecoder;
import io.netty.handler.codec.string.StringEncoder;
import io.netty.util.concurrent.GlobalEventExecutor;
import java.io.File;
import java.io.IOException;
import java.net.SocketAddress;
import java.sql.SQLException;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.List;
import java.util.logging.Level;
import java.util.logging.Logger;

public final class SmartServer {
    private static final EventLoopGroup BOSS_GROUP = new NioEventLoopGroup();
    private static final EventLoopGroup WORKER_GROUP = new NioEventLoopGroup();
    private static final ChannelGroup TCP_IP_CHAN_GROUP;
    private static final ChannelGroup WS_CHAN_GROUP;
    private static final int TCP_IP_PORT = 8080;
    private static final ChannelHandler TCP_IP_HANDLER;
    private static final int WS_PORT = 8081;
    private static final ChannelHandler WS_HANDLER;
    private static final List<SocketAddress> ADMINISTRATORS;
    private static Channel _channel;
    private static Channel _wsChannel;
    public static final int MAX_FRAME_LENGTH = 4194304;
    private static HashSet<ChannelHandlerContext> clientList = new HashSet<ChannelHandlerContext>();
    private static String _currentDirectory = "";
    private SmartServer() {
    }

    public static String getWorkingDirectory() {
        try {
            String se = _currentDirectory;
            char old = '\\';
            char new1 = '/';
            se =  se.replace(old,new1);
            return se;
        } catch (SecurityException var2) {
            SmartLogger.getLogger().log(Level.SEVERE, "Permission to SmartServer Directory not allowed.", var2);
            return "." + File.separator;
        }
    }

    public static void main(String[] args) throws IOException, SQLException {
        _currentDirectory = System.getProperty("user.dir");
        com.spacecode.sdk.SmartLogger.getLogger().setLevel(Level.SEVERE);
        SmartLogger.initialize();
        initializeShutdownHook();
        if(!DeviceHandler.connectDevice()) {
            SmartLogger.getLogger().severe("Unable to connect a device. SmartServer will not start");
        } else {
            Device currentDevice = DeviceHandler.getDevice();
            SmartLogger.getLogger().info(currentDevice.getDeviceType() + ": " + currentDevice.getSerialNumber());
            SmartLogger.getLogger().info("SmartServer is Ready");
            startListening();
        }
    }

    private static void initializeShutdownHook() {
        Runtime.getRuntime().addShutdownHook(new Thread(new Runnable() {
            public void run() {
                DeviceHandler.disconnectDevice();
                SmartServer.stop();
            }
        }));
    }

    private static void startListening() {
        int portTcp;
        try {
        	SmartLogger.getLogger().info(ConfManager.getAppPortTcp());
            portTcp = Integer.parseInt(ConfManager.getAppPortTcp());
            
        } catch (NumberFormatException var12) {
            portTcp = TCP_IP_PORT;
            SmartLogger.getLogger().info("Using default TCP port for TCP/IP channel");
        }

        int portWs;
        try {
            portWs = Integer.parseInt(ConfManager.getAppPortWs());
        } catch (NumberFormatException var11) {
            portWs = WS_PORT;
            SmartLogger.getLogger().info("Using default TCP port for WebSocket channel");
        }

        SmartLogger.getLogger().info(String.format("TCP Ports - TCP/IP: %d",  new Object[]{Integer.valueOf(portTcp)}));

        try {
            ServerBootstrap ie = new ServerBootstrap();

            ((ServerBootstrap)((ServerBootstrap)ie.group(BOSS_GROUP, WORKER_GROUP).channel(NioServerSocketChannel.class)).childHandler(new ChannelInitializer() {
                @Override
                public void initChannel(Channel ch) {
                    ch.pipeline().addLast(new ChannelHandler[]{new DelimiterBasedFrameDecoder(MAX_FRAME_LENGTH, Unpooled.wrappedBuffer(new byte[]{(byte)4}))});
                    ch.pipeline().addLast(new ChannelHandler[]{new StringDecoder(), new StringEncoder()});
                    ch.pipeline().addLast(new ChannelHandler[]{SmartServer.TCP_IP_HANDLER});
                }
            }).option(ChannelOption.SO_BACKLOG, Integer.valueOf(128))).childOption(ChannelOption.SO_KEEPALIVE, Boolean.valueOf(true));
            _channel = ie.bind(portTcp).sync().channel();
/*
            ServerBootstrap wsBootStrap = new ServerBootstrap();
            ((ServerBootstrap)wsBootStrap.group(BOSS_GROUP, WORKER_GROUP).channel(NioServerSocketChannel.class)).childHandler(new ChannelInitializer() {
                public void initChannel(Channel ch) {
                    ch.pipeline().addLast(new ChannelHandler[]{new HttpServerCodec()});
                    ch.pipeline().addLast(new ChannelHandler[]{new HttpObjectAggregator(MAX_FRAME_LENGTH)});
                    ch.pipeline().addLast(new ChannelHandler[]{SmartServer.WS_HANDLER});
                }
            });
*/
            //_wsChannel = wsBootStrap.bind(portWs).sync().channel();
            _channel.closeFuture().sync();
        } catch (InterruptedException var9) {
            Logger.getLogger(SmartServer.class.getName()).log(Level.SEVERE, "InterruptedException during execution of sync().", var9);
        } finally {
            WORKER_GROUP.shutdownGracefully();
            BOSS_GROUP.shutdownGracefully();
        }

    }

    private static void stop() {
        if(_channel != null) {
            _channel.close();
        }

        if(_wsChannel != null) {
            _wsChannel.close();
        }

        WORKER_GROUP.shutdownGracefully();
        BOSS_GROUP.shutdownGracefully();
    }

    public static void addClientChannel(Channel newChannel, ChannelHandler handler) {
        if(handler == WS_HANDLER) {
            WS_CHAN_GROUP.add(newChannel);
        } else if(handler == TCP_IP_HANDLER) {
            TCP_IP_CHAN_GROUP.add(newChannel);
        }

    }

    public static int getClientCount() {
        return clientList.size();
    }

    public static List<String> getClientList() {
        
    	List<String> address = new ArrayList<>();
    		
    	for(ChannelHandlerContext ads: clientList)
    	{
    		address.add(ads.channel().remoteAddress().toString());
    	}
    	return address;
    }

    public static void addClientList(ChannelHandlerContext ctx) {
        SmartServer.clientList.add(ctx);
        SmartLogger.getLogger().info("Client added to list-"+ctx.channel().remoteAddress().toString());
    }

    public static void removeClientList(ChannelHandlerContext ctx) {
        clientList.remove(ctx);
        SmartLogger.getLogger().info("Client removed from list-"+ctx.channel().remoteAddress().toString());
        if(ctx.equals(DeviceHandler.getLastLedStartedClient()))
        {
            DeviceHandler.getDevice().stopLightingTagsLed();
            SmartLogger.getLogger().info("LED stopped since client-"+ctx.channel().remoteAddress().toString()+" disconnected");
            DeviceHandler.setLighting(false);
            
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
        DeviceHandler.removelightFoundTags(ctx);
        if(SmartServer.getClientCount() < 1){        	
            DeviceHandler.getDevice().stopLightingTagsLed();
            DeviceHandler.setLighting(false);
            DeviceHandler.isContinousMode = false;
            DeviceHandler.getDevice().stopScan();
            SmartLogger.getLogger().info("Reading stopped no client connected");
            DeviceHandler.clearClientLightList();
        }          
    }

    public static ChannelFuture sendMessage(ChannelHandlerContext ctx, String... packets) {
        if(ctx == null) {
            return null;
        } else {
            String message = MessageHandler.packetsToFullMessage(packets);
            return message == null?null:(ctx.handler() == WS_HANDLER?ctx.writeAndFlush(new TextWebSocketFrame(message)):ctx.writeAndFlush(message));
        }
    }

    public static ChannelGroupFuture sendAllClients(String... packets) {
        String message = MessageHandler.packetsToFullMessage(packets);
        if(message == null) {
            return null;
        } else {

            ChannelGroupFuture result = TCP_IP_CHAN_GROUP.write(message);
            WS_CHAN_GROUP.write(new TextWebSocketFrame(message));
            TCP_IP_CHAN_GROUP.flush();
            WS_CHAN_GROUP.flush();
            return result;
        }
    }

    static {
        TCP_IP_CHAN_GROUP = new DefaultChannelGroup(GlobalEventExecutor.INSTANCE);
        WS_CHAN_GROUP = new DefaultChannelGroup(GlobalEventExecutor.INSTANCE);
        TCP_IP_HANDLER = new SmartServerHandler();
        WS_HANDLER = new WebSocketHandler();
        ADMINISTRATORS = new ArrayList();
    }
}
