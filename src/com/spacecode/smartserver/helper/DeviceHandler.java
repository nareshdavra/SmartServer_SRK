//
// Source code recreated from a .class file by IntelliJ IDEA
// (powered by Fernflower decompiler)
//

package com.spacecode.smartserver.helper;

import com.spacecode.sdk.device.*;
import com.spacecode.sdk.device.data.*;

import com.spacecode.sdk.device.event.*;
import com.spacecode.sdk.device.module.authentication.FingerprintReader;
import com.spacecode.sdk.device.module.authentication.FingerprintReaderException;
import com.spacecode.sdk.user.*;
import com.spacecode.sdk.user.data.AccessType;
import com.spacecode.smartserver.SmartServer;

import io.netty.channel.ChannelHandlerContext;

import java.net.SocketAddress;
import java.util.*;
import java.util.Map.Entry;
//import java.util.concurrent.;
import java.util.logging.Level;

public final class DeviceHandler {
    private static volatile Device DEVICE;
    private static volatile boolean RECORD_INVENTORY = true;
    private static boolean SERIAL_PORT_FORWARDING = false;
    private static boolean _isLighting = false;
    private static boolean _isInScan = false;
    private static int setCliListSize = 0;
    private static boolean foundHashMapHasMore = false;
    private static Map<ChannelHandlerContext,List<String>> clientSpecificLightList = new HashMap<ChannelHandlerContext,List<String>>();// ConcurrentHashMap<ChannelHandlerContext,List<String>>(); //new ConcurrentHashMap<String,ChannelHandlerContext>();
    private static Map<ChannelHandlerContext, List<String>> foundHashmap = new HashMap<ChannelHandlerContext, List<String>>();
    private static Map<ChannelHandlerContext, List<String>> remainedHashmap = new HashMap<ChannelHandlerContext, List<String>>();
    private static ChannelHandlerContext ledStartedClinet =null;
    
    private DeviceHandler() {
    }

    public static boolean isContinousMode;

    public static synchronized boolean connectDevice() {
        if(DEVICE != null) {
            return true;
        } else {
            Map pluggedDevices = Device.getPluggedDevices();
            if(!pluggedDevices.isEmpty() && pluggedDevices.size() <= 1) {
                PluggedDevice deviceInfo = (PluggedDevice)((Entry)pluggedDevices.entrySet().iterator().next()).getValue();

                try {
                    DEVICE = new Device((String)null, deviceInfo.getSerialPort());
                    DEVICE.addListener(new DeviceHandler.SmartEventHandler());
                    return true;
                } catch (DeviceCreationException var3) {
                    SmartLogger.getLogger().log(Level.INFO, "Unable to instantiate a device.", var3);
                    return false;
                }
            } else {
                SmartLogger.getLogger().warning("0 or more than 1 device detected.");
                return false;
            }
        }
    }

    public static synchronized void disconnectDevice() {
        if(DEVICE != null) {
            DEVICE.release();
            DEVICE = null;
        }

    }

    public static boolean reconnectDevice() {
        boolean deviceConnected = false;
        long initialTimestamp = System.currentTimeMillis();

        while(!SERIAL_PORT_FORWARDING && System.currentTimeMillis() - initialTimestamp < 3600000L) {
            SmartLogger.getLogger().info("Reconnecting Device...");
            deviceConnected = connectDevice();
            if(deviceConnected) {
                SmartServer.sendAllClients(new String[]{"event_status_changed", DeviceStatus.READY.name()});
                break;
            }

            try {
                Thread.sleep(5000L);
            } catch (InterruptedException var4) {
                SmartLogger.getLogger().log(Level.WARNING, "Interrupted while trying to reconnect Device.", var4);
                break;
            }
        }

        return deviceConnected;
    }

    public static  Device getDevice() {
        return DEVICE;
    }

    public static synchronized DeviceStatus getDeviceStatus() {return DEVICE.getStatus();}

    public static void connectModules() {
        if(DEVICE == null) {
            SmartLogger.getLogger().warning("Unable to connect modules, the device is not initialized.");
        } else {
            String fprMaster = ConfManager.getDevFprMaster();
            String fprSlave = ConfManager.getDevFprSlave();

            try {
                if(fprMaster != null && !fprMaster.trim().isEmpty()) {
                    if(fprSlave != null && !fprSlave.trim().isEmpty()) {
                        if(FingerprintReader.connectFingerprintReaders(2) != 2) {
                            SmartLogger.getLogger().warning("Couldn\'t initialize the two fingerprint readers.");
                        } else if(!DEVICE.addFingerprintReader(fprMaster, true) || !DEVICE.addFingerprintReader(fprSlave, false)) {
                            SmartLogger.getLogger().warning("Couldn\'t connect the two fingerprint readers.");
                        }
                    } else if(FingerprintReader.connectFingerprintReader() != 1) {
                        SmartLogger.getLogger().warning("Couldn\'t initialize the fingerprint reader.");
                    } else if(!DEVICE.addFingerprintReader(fprMaster, true)) {
                        SmartLogger.getLogger().warning("Couldn\'t connect the fingerprint reader.");
                    }
                }
            } catch (FingerprintReaderException var4) {
                SmartLogger.getLogger().log(Level.INFO, "An unexpected error occurred during fingerprint readers initialization.", var4);
            }

            String brMaster = ConfManager.getDevBrMaster();
            String brSlave = ConfManager.getDevBrSlave();
            if(brMaster != null && !brMaster.trim().isEmpty()) {
                if(!DEVICE.addBadgeReader(brMaster, true)) {
                    SmartLogger.getLogger().warning("Unable to add Master Badge Reader on " + brMaster);
                }

                if(brSlave != null && !brSlave.trim().isEmpty() && !DEVICE.addBadgeReader(brSlave, false)) {
                    SmartLogger.getLogger().warning("Unable to add Slave Badge Reader on " + brSlave);
                }
            }

            connectProbeIfEnabled();
        }
    }

    private static void connectProbeIfEnabled() {
        if(ConfManager.isDevTemperature()) {
            int measurementDelay = ConfManager.getDevTemperatureDelay();
            double measurementDelta = ConfManager.getDevTemperatureDelta();
            measurementDelay = measurementDelay == -1?60:measurementDelay;
            measurementDelta = measurementDelta == -1.0D?0.3D:measurementDelta;
            if(!DEVICE.addTemperatureProbe("tempProbe1", measurementDelay, measurementDelta)) {
                SmartLogger.getLogger().warning("Unable to add the Temperature probe.");
            }
        }

    }
    

    public static List<String> setClientLightList(ChannelHandlerContext ctx,List<String> lightListParam){
        if(lightListParam != null && lightListParam.size() > 0) {

        	List<String> client = new ArrayList<>();
        	HashSet<String> lightList = new HashSet<>(lightListParam); 
        	List<String> returnListclient = new ArrayList<>();
        	HashMap<ChannelHandlerContext,List<String>> checklist =null;
            synchronized (ctx) {        	
		        checklist = new HashMap<>();
		        checklist.putAll(clientSpecificLightList);
		        client.addAll(lightList);
            }
            
            try {
                for (ChannelHandlerContext xtc : checklist.keySet()) {
                    for (String tag : lightList) {
                        if (checklist.get(xtc).contains(tag) == true) {
                            client.remove(tag);
                            returnListclient.add(tag);
                        }
                    }
                }
            }
            catch(Exception ee)
            {
                SmartLogger.getLogger().severe(ee.getMessage());
                ee.printStackTrace();
            }
            SmartLogger.getLogger().info("modified list from"+ ctx.channel().remoteAddress().toString()+":"+ StringOPs.getCommaSaparetedString(client));

            synchronized (ctx) {
            clientSpecificLightList.put(ctx, client);
            }
            return  returnListclient;
        }
        else
        {
            return null;
        }
    }

    public static boolean IsThereMoreFoundTags()
    {
        return foundHashMapHasMore;
    }
    public static void removelightFoundTags(ChannelHandlerContext cctx)    
    {
    	synchronized (cctx) {    		
    		clientSpecificLightList.remove(cctx);
    		foundHashmap.remove(cctx);
    		remainedHashmap.remove(cctx);
    	}
    }

    public static void lightFoundTags()
    {
        Inventory lastInventory = DEVICE.getLastInventory();
        foundHashmap.clear();
        remainedHashmap.clear();
        List<String> found = new ArrayList<String>();

        for (ChannelHandlerContext xtc : clientSpecificLightList.keySet()) {
            found.addAll(clientSpecificLightList.get(xtc));
        }


        found.retainAll(lastInventory.getTagsAll());
        if (found.size() > 0) {

            synchronized (DEVICE) {

                for (ChannelHandlerContext xtc : clientSpecificLightList.keySet()) {
                    List<String> _found = new ArrayList<String>(clientSpecificLightList.get(xtc));
                    List<String> _notFound = new ArrayList<String>(clientSpecificLightList.get(xtc));
                    _found.retainAll(found);
                    _notFound.removeAll(found);
                    if (_found.size() > 0) {
                        foundHashmap.put(xtc, _found);
                    }
                    if (_notFound.size() > 0) {
                        remainedHashmap.put(xtc, _notFound);
                    }
                }
            }

            foundHashMapHasMore = (foundHashmap.keySet().size() > 1)?true:false;

            Entry<ChannelHandlerContext, List<String>> entry = foundHashmap.entrySet().iterator().next();

            boolean res = DEVICE.startLightingTagsLed(foundHashmap.get(entry.getKey()));
            SmartLogger.getLogger().info("LED Started for " + entry.getKey().channel().remoteAddress().toString() + " tags : " + StringOPs.getCommaSaparetedString(foundHashmap.get(entry.getKey())));
            ledStartedClinet  = entry.getKey();
            if (res == true) {
                SmartServer.sendMessage(entry.getKey(), new String[]{"event_led_found", StringOPs.getCommaSaparetedString(entry.getValue())});
            } else {
                remainedHashmap.put(entry.getKey(), foundHashmap.get(entry.getValue()));
            }

            foundHashmap.remove(entry.getKey());

            for(ChannelHandlerContext xtct: foundHashmap.keySet()) {
                remainedHashmap.put(xtct,foundHashmap.get(xtct));
            }

            synchronized (DEVICE)
            {
                clientSpecificLightList.clear();
                clientSpecificLightList.putAll(remainedHashmap);
            }

        } else {
            DEVICE.requestScan();
        }

    }


    public static ChannelHandlerContext getLastLedStartedClient(){
        return ledStartedClinet;
    }
    
    public static void clearClientLightList(){
        clientSpecificLightList.clear();
        clientSpecificLightList.clear();
        setCliListSize =0;
    }

    public static void reloadTemperatureProbe() {
        DEVICE.disconnectTemperatureProbe();
        connectProbeIfEnabled();
    }

    public static void setForwardingSerialPort(boolean state) {
        SERIAL_PORT_FORWARDING = state;
    }

    public static boolean isAvailable() {
        return !SERIAL_PORT_FORWARDING && DEVICE != null;
    }

    public static void setRecordInventory(boolean state) {
        RECORD_INVENTORY = state;
    }

    public static boolean getRecordInventory() {
        return RECORD_INVENTORY;
    }

    public static boolean isLighting() {
        return _isLighting;
    }

    public static void setLighting(boolean _isLighting) {
        DeviceHandler._isLighting = _isLighting;
    }

    public static boolean isInScan() {
        return _isInScan;
    }

    public synchronized static void setInScan(boolean _isInScan) {
        DeviceHandler._isInScan = _isInScan;
    }

    static class SmartEventHandler implements BasicEventHandler, ScanEventHandler, DoorEventHandler, AccessControlEventHandler, AccessModuleEventHandler, TemperatureEventHandler, LedEventHandler, MaintenanceEventHandler {
        SmartEventHandler() {
        }

        public void deviceDisconnected() {
            SmartLogger.getLogger().info("Device Disconnected...");
            SmartServer.sendAllClients(new String[]{"event_device_disconnected"});
            DeviceHandler.DEVICE = null;
            DeviceHandler.reconnectDevice();
        }

        public void doorOpened() {
            SmartServer.sendAllClients(new String[]{"event_door_opened"});
        }

        public void doorClosed() {
            SmartServer.sendAllClients(new String[]{"event_door_closed"});
        }

        public void doorOpenDelay() {
            SmartServer.sendAllClients(new String[]{"event_door_open_delay"});
        }

        public void scanStarted() {
            setInScan(true);
            SmartServer.sendAllClients(new String[]{"event_scan_started"});
            /*
            synchronized (this) {
                if (clientSpecificLightList.keySet().size() > setCliListSize) {
                    clientSpecificLightList.clear();
                    clientSpecificLightList.putAll(clientSpecificLightList);
                    setCliListSize = clientSpecificLightList.keySet().size();
                }
            }*/
        }

        public void scanCancelledByHost() {
            SmartServer.sendAllClients(new String[]{"event_scan_cancelled_by_host"});
        }

        public void scanCompleted() {
            if(isContinousMode) {
                 DeviceHandler.lightFoundTags();
            }
            else
            {
                setInScan(false);
                SmartLogger.getLogger().info("scan_completed");
                SmartServer.sendAllClients(new String[]{"event_scan_completed"});
            }
        }

        public void scanFailed() {
            SmartServer.sendAllClients(new String[]{"event_scan_failed"});
        }

        public void tagAdded(String tagUID) {
            if(tagUID != null)
            {
                SmartServer.sendAllClients(new String[]{"event_tag_added", tagUID});
            }
        }

        public void authenticationSuccess(User grantedUser, AccessType accessType, boolean isMaster) {
            SmartServer.sendAllClients(new String[]{"event_authentication_success", grantedUser.serialize(), accessType.name(), String.valueOf(isMaster)});
        }

        public void authenticationFailure(User grantedUser, AccessType accessType, boolean isMaster) {
            SmartServer.sendAllClients(new String[]{"event_authentication_failure", grantedUser.serialize(), accessType.name(), String.valueOf(isMaster)});
        }

        public void fingerTouched(boolean isMaster) {
            SmartServer.sendAllClients(new String[]{"event_finger_touched", Boolean.valueOf(isMaster).toString()});
        }

        public void fingerprintEnrollmentSample(byte sampleNumber) {
            SmartServer.sendAllClients(new String[]{"event_enrollment_sample", String.valueOf(sampleNumber)});
        }

        public void badgeReaderConnected(boolean isMaster) {
            SmartLogger.getLogger().info("Badge reader (" + (isMaster?"Master":"Slave") + ") connected.");
        }

        public void badgeReaderDisconnected(boolean isMaster) {
            SmartLogger.getLogger().info("Badge reader (" + (isMaster?"Master":"Slave") + ") disconnected.");
        }

        public void badgeScanned(String badgeNumber) {
            SmartServer.sendAllClients(new String[]{"event_badge_scanned", badgeNumber});
        }

        public void scanCancelledByDoor() {
            SmartLogger.getLogger().info("Scan has been cancelled because someone opened the door.");
            SmartServer.sendAllClients(new String[]{"event_scan_cancelled_by_door"});
        }

        public void temperatureMeasure(double value) {
            SmartServer.sendAllClients(new String[]{"event_temperature_measure", String.valueOf(value)});
        }

        public void lightingStarted(List<String> tagsLeft) {
            setLighting(true);
            ArrayList<String> responsePackets = new ArrayList<String>();
            responsePackets.add("event_lighting_started");
            responsePackets.addAll(tagsLeft);
            SmartServer.sendAllClients((String[])responsePackets.toArray(new String[responsePackets.size()]));
        }

        public void lightingStopped() {
            setLighting(false);
            SmartServer.sendAllClients(new String[]{"event_lighting_stopped"});
        }

        public void deviceStatusChanged(DeviceStatus status) {
            SmartServer.sendAllClients(new String[]{"event_status_changed", status.name()});
        }

        public void flashingProgress(int rowNumber, int rowCount) {
            SmartServer.sendAllClients(new String[]{"event_flashing_progress", String.valueOf(rowNumber), String.valueOf(rowCount)});
        }

        public void correlationSample(int correlation, int phaseShift) {
        }

        public void correlationSampleSeries(short[] presentSamples, short[] missingSamples) {
            if(presentSamples != null && missingSamples != null) {
                ArrayList responsePackets = new ArrayList();
                responsePackets.add("event_correlation_series");
                responsePackets.add("present");
                short[] arr$ = presentSamples;
                int len$ = presentSamples.length;

                int i$;
                short missingSample;
                for(i$ = 0; i$ < len$; ++i$) {
                    missingSample = arr$[i$];
                    responsePackets.add(String.valueOf(missingSample));
                }

                responsePackets.add("missing");
                arr$ = missingSamples;
                len$ = missingSamples.length;

                for(i$ = 0; i$ < len$; ++i$) {
                    missingSample = arr$[i$];
                    responsePackets.add(String.valueOf(missingSample));
                }

                SmartServer.sendAllClients((String[])responsePackets.toArray(new String[responsePackets.size()]));
            }
        }

    }
}
