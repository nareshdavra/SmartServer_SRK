package com.spacecode.smartserver;

import com.spacecode.smartserver.SmartServer;
import com.spacecode.smartserver.command.ClientCommandException;
import com.spacecode.smartserver.command.ClientCommandRegister;
import com.spacecode.smartserver.helper.SmartLogger;
import io.netty.buffer.ByteBuf;
import io.netty.buffer.Unpooled;
import io.netty.channel.ChannelFuture;
import io.netty.channel.ChannelFutureListener;
import io.netty.channel.ChannelHandlerContext;
import io.netty.channel.SimpleChannelInboundHandler;
import io.netty.channel.ChannelHandler.Sharable;
import io.netty.handler.codec.http.DefaultFullHttpResponse;
import io.netty.handler.codec.http.FullHttpRequest;
import io.netty.handler.codec.http.FullHttpResponse;
import io.netty.handler.codec.http.HttpHeaders;
import io.netty.handler.codec.http.HttpMethod;
import io.netty.handler.codec.http.HttpResponseStatus;
import io.netty.handler.codec.http.HttpVersion;
import io.netty.handler.codec.http.websocketx.CloseWebSocketFrame;
import io.netty.handler.codec.http.websocketx.ContinuationWebSocketFrame;
import io.netty.handler.codec.http.websocketx.TextWebSocketFrame;
import io.netty.handler.codec.http.websocketx.WebSocketFrame;
import io.netty.handler.codec.http.websocketx.WebSocketServerHandshaker;
import io.netty.handler.codec.http.websocketx.WebSocketServerHandshakerFactory;
import io.netty.util.CharsetUtil;
import java.util.logging.Level;

@Sharable
class WebSocketHandler extends SimpleChannelInboundHandler {

   private WebSocketServerHandshaker _handshaker;
   private static final ClientCommandRegister COMMAND_REGISTER = new ClientCommandRegister();
   private final StringBuilder _continuousBuffer = new StringBuilder();


   public void channelActive(ChannelHandlerContext ctx) {
      SmartServer.addClientChannel(ctx.channel(), ctx.handler());
      SmartLogger.getLogger().info("Connection from " + ctx.channel().remoteAddress());
   }

   public void channelRead0(ChannelHandlerContext ctx, Object msg) {
      if(msg instanceof FullHttpRequest) {
         this.handleHttpRequest(ctx, (FullHttpRequest)msg);
      } else if(msg instanceof WebSocketFrame) {
         this.handleWebSocketFrame(ctx, (WebSocketFrame)msg);
      }

   }

   public void channelReadComplete(ChannelHandlerContext ctx) {
      ctx.flush();
   }

   private void handleHttpRequest(ChannelHandlerContext ctx, FullHttpRequest req) {
      if(!req.getDecoderResult().isSuccess()) {
         sendHttpResponse(ctx, req, new DefaultFullHttpResponse(HttpVersion.HTTP_1_1, HttpResponseStatus.BAD_REQUEST));
      } else if(req.getMethod() != HttpMethod.GET) {
         sendHttpResponse(ctx, req, new DefaultFullHttpResponse(HttpVersion.HTTP_1_1, HttpResponseStatus.FORBIDDEN));
      } else {
         WebSocketServerHandshakerFactory wsFactory = new WebSocketServerHandshakerFactory("ws://" + req.headers().get("Host"), (String)null, false, 4194304);
         this._handshaker = wsFactory.newHandshaker(req);
         if(this._handshaker == null) {
            WebSocketServerHandshakerFactory.sendUnsupportedVersionResponse(ctx.channel());
         } else {
            this._handshaker.handshake(ctx.channel(), req);
         }

      }
   }

   private void handleWebSocketFrame(ChannelHandlerContext ctx, WebSocketFrame frame) {
      if(frame instanceof CloseWebSocketFrame) {
         this._handshaker.close(ctx.channel(), (CloseWebSocketFrame)frame.retain());
      } else {
         if(frame instanceof TextWebSocketFrame) {
            this._continuousBuffer.append(((TextWebSocketFrame)frame).text());
         } else {
            if(!(frame instanceof ContinuationWebSocketFrame)) {
               SmartLogger.getLogger().severe("Invalid WebSocketFrame not handled: " + frame.getClass());
               return;
            }

            this._continuousBuffer.append(((ContinuationWebSocketFrame)frame).text());
         }

         if(frame.isFinalFragment()) {
            String request = this._continuousBuffer.toString();
            this._continuousBuffer.setLength(0);
            if(!request.trim().isEmpty()) {
               String[] parameters = request.split(Character.toString('\u001c'));
               SmartLogger.getLogger().info(ctx.channel().remoteAddress().toString() + " - " + parameters[0]);

               try {
                  COMMAND_REGISTER.execute(ctx, parameters);
               } catch (ClientCommandException var6) {
                  SmartLogger.getLogger().log(Level.SEVERE, "ClientCommand exception occurred.", var6);
               }

            }
         }
      }
   }

   private static void sendHttpResponse(ChannelHandlerContext ctx, FullHttpRequest req, FullHttpResponse res) {
      if(res.getStatus().code() != 200) {
         ByteBuf f = Unpooled.copiedBuffer(res.getStatus().toString(), CharsetUtil.UTF_8);
         res.content().writeBytes(f);
         f.release();
         HttpHeaders.setContentLength(res, (long)res.content().readableBytes());
      }

      ChannelFuture f1 = ctx.channel().writeAndFlush(res);
      if(!HttpHeaders.isKeepAlive(req) || res.getStatus().code() != 200) {
         f1.addListener(ChannelFutureListener.CLOSE);
      }

   }

   public void exceptionCaught(ChannelHandlerContext ctx, Throwable cause) {
      SmartLogger.getLogger().log(Level.WARNING, "Exception caught by handler", cause);
      ctx.close();
   }

}
