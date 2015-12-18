package com.spacecode.smartserver.command;

import com.spacecode.sdk.device.data.DeviceStatus;
import com.spacecode.sdk.device.data.ScanOption;
import com.spacecode.smartserver.SmartServer;
import com.spacecode.smartserver.command.ClientCommand;
import com.spacecode.smartserver.command.CommandContract;
import com.spacecode.smartserver.helper.DeviceHandler;
import com.spacecode.smartserver.helper.SmartLogger;
import com.spacecode.smartserver.helper.StringOPs;
import io.netty.channel.ChannelHandlerContext;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.logging.Level;

/**
 * Created by MY on 23/10/2015.
 */
public class CmdSetLightList extends ClientCommand {
    public void execute(ChannelHandlerContext ctx, String[] parameters) {
        List<String> lLight= new ArrayList(Arrays.asList(parameters));
        SmartLogger.getLogger().info("original list from"+ ctx.channel().remoteAddress().toString()+":"+ StringOPs.getCommaSaparetedString(lLight));
        List<String> result = DeviceHandler.setClientLightList(ctx,lLight);
        if(result.size() > 0){
            SmartServer.sendMessage(ctx, new String[]{"event_setModifiedlightlist", StringOPs.getCommaSaparetedString (result)});        	
        }
        SmartServer.sendMessage(ctx, new String[]{"setlightlist"});
    }
}
