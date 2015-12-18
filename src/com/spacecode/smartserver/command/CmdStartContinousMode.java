package com.spacecode.smartserver.command;

import com.spacecode.sdk.device.data.ScanOption;
import com.spacecode.smartserver.helper.DeviceHandler;
import com.spacecode.smartserver.helper.SmartLogger;
import io.netty.channel.ChannelHandlerContext;
import java.util.ArrayList;
import java.util.logging.Level;

@CommandContract(
        deviceRequired = true,
        responseIfInvalid = "event_scan_failed",
        respondToAllIfInvalid = true
)
public class CmdStartContinousMode
        extends ClientCommand {
    @Override
    public synchronized void execute(ChannelHandlerContext ctx, String[] parameters) {

        if (DeviceHandler.isLighting()) {
            //SmartLogger.getLogger().warning("Trying to start a scan whereas the Device 'LEDon'!");
            SmartLogger.getLogger().info("Start Continous scan from " + ctx.channel().remoteAddress().toString() + ": Device LED on");

            return;
        }
        if (DeviceHandler.isContinousMode == true) {
            //SmartLogger.getLogger().warning("Trying to start a scan whereas the Device is already 'SCANNING'!");
            SmartLogger.getLogger().info("Start Continous scan  from "+ctx.channel().remoteAddress().toString()+ ": Device is already SCANNING");
            return;
        }
        ArrayList<ScanOption> scanOptions = new ArrayList<ScanOption>();
        if (parameters.length > 0) {
            String[] arr$ = parameters;
            int len$ = parameters.length;
            for (int i$ = 0; i$ < len$; ++i$) {
                String option = arr$[i$];
                try {
                    scanOptions.add(ScanOption.valueOf(option));
                    continue;
                }
                catch (IllegalArgumentException var9) {
                    SmartLogger.getLogger().log(Level.WARNING, "Invalid ScanOption provided: " + option, var9);
                }
            }
        }
        DeviceHandler.setRecordInventory(!scanOptions.contains((Object)ScanOption.NO_RECORD));
        DeviceHandler.isContinousMode = true;
        if(scanOptions.size() > 0) {
            DeviceHandler.getDevice().requestScan((ScanOption[]) scanOptions.toArray(new ScanOption[scanOptions.size()]));
        }
        else{
            DeviceHandler.getDevice().requestScan();
        }
    }
}

