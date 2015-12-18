package com.spacecode.smartserver.command;

import com.spacecode.sdk.device.data.DeviceStatus;
import com.spacecode.sdk.device.data.ScanOption;
import com.spacecode.smartserver.command.ClientCommand;
import com.spacecode.smartserver.command.CommandContract;
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
public class CmdScan extends ClientCommand {

   public void execute(ChannelHandlerContext ctx, String[] parameters) {

           if (DeviceHandler.isLighting()) {
               SmartLogger.getLogger().warning("Trying to start a scan whereas the Device 'LEDon'!");
               return;
           } else if (DeviceHandler.getDeviceStatus() == DeviceStatus.SCANNING) {
               SmartLogger.getLogger().warning("Trying to start a scan whereas the Device is already 'SCANNING'!");
               return;
           } else {
               ArrayList scanOptions = new ArrayList();
               if (parameters.length > 0) {
                   String[] arr$ = parameters;
                   int len$ = parameters.length;

                   for (int i$ = 0; i$ < len$; ++i$) {
                       String option = arr$[i$];

                       try {
                           scanOptions.add(ScanOption.valueOf(option));
                       } catch (IllegalArgumentException var9) {
                           SmartLogger.getLogger().log(Level.WARNING, "Invalid ScanOption provided: " + option, var9);
                       }
                   }
               }

               DeviceHandler.setRecordInventory(!scanOptions.contains(ScanOption.NO_RECORD));
               DeviceHandler.getDevice().requestScan((ScanOption[]) scanOptions.toArray(new ScanOption[scanOptions.size()]));
           }

   }
}
