//
// Source code recreated from a .class file by IntelliJ IDEA
// (powered by Fernflower decompiler)
//

package com.spacecode.smartserver.command;

import com.spacecode.smartserver.SmartServer;
import com.spacecode.smartserver.command.ClientCommand;
import com.spacecode.smartserver.command.ClientCommandException;
import com.spacecode.smartserver.command.CmdDeviceStatus;
import com.spacecode.smartserver.command.CmdDisconnect;
import com.spacecode.smartserver.command.CmdInitialization;
import com.spacecode.smartserver.command.CmdLastInventory;
import com.spacecode.smartserver.command.CmdRewriteUid;
import com.spacecode.smartserver.command.CmdScan;
import com.spacecode.smartserver.command.CmdStartLighting;
import com.spacecode.smartserver.command.CmdStopLighting;
import com.spacecode.smartserver.command.CmdStopScan;
import com.spacecode.smartserver.command.CommandContract;
import com.spacecode.smartserver.helper.DeviceHandler;
import com.spacecode.smartserver.helper.SmartLogger;
import io.netty.channel.ChannelHandlerContext;
import java.lang.annotation.Annotation;
import java.net.SocketAddress;
import java.util.Arrays;
import java.util.HashMap;
import java.util.Map;
import java.util.logging.Level;

public final class ClientCommandRegister extends ClientCommand {
    private final Map<String, ClientCommand> _commands = new HashMap();
    static final int DELAY_BETWEEN_EXEC = 500;
    private long _lastExecTimestamp;
    private String[] _lastExecPackets = new String[]{""};
    private SocketAddress _lastSender = null;
    public static boolean IsLightingStarted = false;

    public ClientCommandRegister() {
        this._commands.put("devicestatus", new CmdDeviceStatus());
        this._commands.put("disconnect", new CmdDisconnect());
        this._commands.put("initialization", new CmdInitialization());
        this._commands.put("lastinventory", new CmdLastInventory());
        this._commands.put("rewriteuid", new CmdRewriteUid());
        this._commands.put("scan", new CmdScan());
        this._commands.put("startlighting", new CmdStartLighting());
        this._commands.put("stoplighting", new CmdStopLighting());
        this._commands.put("stopscan", new CmdStopScan());
        this._commands.put("startcontinousscan", new CmdStartContinousMode());
        this._commands.put("stopcontinousscan", new CmdStopContinousMode());
        this._commands.put("setlightlist", new CmdSetLightList());
        this._commands.put("stopscan", new CmdStopScan());
    }

    public boolean addCommand(String name, ClientCommand command) {
        if(name != null && command != null && !name.trim().isEmpty()) {
            if(this._commands.containsKey(name)) {
                return false;
            } else {
                this._commands.put(name, command);
                return true;
            }
        } else {
            return false;
        }
    }

    public void execute(ChannelHandlerContext ctx, String[] parameters) throws ClientCommandException {
        String requestCode = parameters[0];
        ClientCommand cmd = (ClientCommand)this._commands.get(requestCode);
        if(cmd == null) {
            throw new ClientCommandException("Unknown Command: " + requestCode);
        } else {

            boolean executeCommand = true;
            long currentTimestamp = System.currentTimeMillis();
            if(requestCode.equals(this._lastExecPackets[0]) && this._lastSender == ctx.channel().remoteAddress() && parameters.length > 0 && this._lastExecPackets.length == parameters.length) {
                executeCommand = false;

                for(int i = 1; i < parameters.length; ++i) {
                    if(!parameters[i].equals(this._lastExecPackets[i])) {
                        executeCommand = true;
                        break;
                    }
                }

                if(!executeCommand) {
                    executeCommand = currentTimestamp - this._lastExecTimestamp > 500L;
                }
            }

            executeCommand= (IsLightingStarted==false) && executeCommand;

            this._lastExecTimestamp = currentTimestamp;
            this._lastExecPackets = (String[])Arrays.copyOfRange(parameters, 0, parameters.length);
            this._lastSender = ctx.channel().remoteAddress();
            if(executeCommand) {
                this.executeOrFail(cmd, ctx, requestCode, (String[])Arrays.copyOfRange(parameters, 1, parameters.length));
            }
        }
    }

    public void executeOrFail(ClientCommand cmd, ChannelHandlerContext ctx, String requestCode, String[] cmdParams) throws ClientCommandException {
        Annotation[] arr$ = cmd.getClass().getAnnotations();
        int len$ = arr$.length;

        for(int i$ = 0; i$ < len$; ++i$) {
            Annotation annotation = arr$[i$];
            if(annotation instanceof CommandContract) {
                CommandContract contract = (CommandContract)annotation;
                boolean logException = true;

                try {
                    if(cmdParams.length < contract.paramCount() || contract.strictCount() && cmdParams.length != contract.paramCount()) {
                        throw new ClientCommandException("Invalid number of parameters [" + requestCode + "]");
                    }

                    if(contract.deviceRequired() && !DeviceHandler.isAvailable()) {
                        logException = false;
                        throw new ClientCommandException("Device not available [" + requestCode + "]");
                    }
                } catch (ClientCommandException var12) {
                    if(logException) {
                        SmartLogger.getLogger().log(Level.WARNING, "An error occurred while executing a command [" + requestCode + "]", var12);
                    }

                    if(!contract.noResponseWhenInvalid()) {
                        if(contract.respondToAllIfInvalid()) {
                            SmartServer.sendAllClients(new String[]{contract.responseIfInvalid()});
                        } else {
                            SmartServer.sendMessage(ctx, new String[]{requestCode, contract.responseIfInvalid()});
                        }
                    }

                    return;
                }
            }
        }

        cmd.execute(ctx, cmdParams);
    }

    static class AppCode {
        static final String BR_SERIAL = "brserial";
        static final String FPR_SERIAL = "fprserial";
        static final String FLASH_FIRMWARE = "flashfirmware";
        static final String HOSTNAME = "hostname";
        static final String NETWORK_SETTINGS = "networksettings";
        static final String SET_BR_SERIAL = "setbrserial";
        static final String SET_FPR_SERIAL = "setfprserial";
        static final String SET_NETWORK = "setnetworksettings";
        static final String SERIAL_BRIDGE = "serialbridge";
        static final String SIGN_IN_ADMIN = "signinadmin";
        static final String START_UPDATE = "startupdate";
        static final String UPDATE_REPORT = "updatereport";
        static final String RFID_AXIS_COUNT = "rfidaxiscount";
        static final String RFID_CALIBRATE = "rfidcalibrate";
        static final String RFID_DEC_FREQUENCY = "rfiddecfrequency";
        static final String RFID_DUTY_CYCLE = "rfiddutycycle";
        static final String RFID_FREQUENCY = "rfidfrequency";
        static final String RFID_INC_FREQUENCY = "rfidincfrequency";
        static final String RFID_SAVE_DUTY_CYCLE = "rfidsavedutycycle";
        static final String RFID_SELECT_AXIS = "rfidselectaxis";
        static final String RFID_SET_DOOR_STATE = "rfidsetdoorstate";
        static final String RFID_SET_DUTY_CYCLE = "rfidsetdutycycle";
        static final String RFID_SET_THRESHOLD = "rfidsetthreshold";
        static final String RFID_THRESHOLD = "rfidthreshold";
        static final String RFID_THRESHOLD_SAMPLING = "rfidthresholdsampling";

        AppCode() {
        }
    }
}
