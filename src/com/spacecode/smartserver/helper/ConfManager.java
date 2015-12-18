//
// Source code recreated from a .class file by IntelliJ IDEA
// (powered by Fernflower decompiler)
//

package com.spacecode.smartserver.helper;

import com.spacecode.sdk.device.module.TemperatureProbe.Settings;
import com.spacecode.sdk.network.DbConfiguration;
import com.spacecode.smartserver.SmartServer;
import com.spacecode.smartserver.helper.SmartLogger;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStreamReader;
import java.util.Properties;
import java.util.logging.Level;

public class ConfManager {
    private final Properties configProp;
    private static final String CONFIG_FILENAME = "smartserver.properties";
    public static String CONFIG_FILE = SmartServer.getWorkingDirectory() +"/"+ CONFIG_FILENAME;
    public static final String DB_HOST = "db_host";
    public static final String DB_PORT = "db_port";
    public static final String DB_DBMS = "db_dbms";
    public static final String DB_NAME = "db_name";
    public static final String DB_USER = "db_user";
    public static final String DB_PASSWORD = "db_password";
    public static final String APP_PORT_TCP = "app_port_tcp";
    public static final String APP_PORT_WS = "app_port_ws";
    public static final String DEV_BR_MASTER = "dev_br_master";
    public static final String DEV_BR_SLAVE = "dev_br_slave";
    public static final String DEV_FPR_MASTER = "dev_fpr_master";
    public static final String DEV_FPR_SLAVE = "dev_fpr_slave";
    public static final String DEV_TEMPERATURE = "dev_temperature";
    public static final String DEV_TEMPERATURE_DELTA = "dev_t_delta";
    public static final String DEV_TEMPERATURE_DELAY = "dev_t_delay";

    private ConfManager() {    	
    	//CONFIG_FILE = SmartServer.getWorkingDirectory() + CONFIG_FILENAME;
    	this.configProp = new Properties();

        try {
            File ioe = new File(CONFIG_FILE);
            if(ioe.createNewFile()) {
                SmartLogger.getLogger().warning("Configuration file was not present. Now created.");
            }

            FileInputStream fis = new FileInputStream(CONFIG_FILE);
            InputStreamReader isr = new InputStreamReader(fis, "UTF-8");
            this.configProp.load(isr);
            isr.close();
            fis.close();
        } catch (IOException var4) {
            SmartLogger.getLogger().log(Level.SEVERE, "An I/O error occurred while loading properties.", var4);
        }

    }

    private String getProperty(String key) {
        String propertyValue = this.configProp.getProperty(key);
        return propertyValue == null?null:propertyValue;
    }

    private boolean setProperty(String key, String value) {
        this.configProp.setProperty(key, value == null?"":value);

        try {
            FileOutputStream ioe = new FileOutputStream(CONFIG_FILE);
            this.configProp.store(ioe, (String)null);
            ioe.close();
            return true;
        } catch (IOException var4) {
            SmartLogger.getLogger().log(Level.SEVERE, "An I/O error occurred while updating properties.", var4);
            return false;
        }
    }

    public static String getDbHost() {
        return ConfManager.LazyHolder.INSTANCE.getProperty("db_host");
    }

    public static String getDbPort() {
        return ConfManager.LazyHolder.INSTANCE.getProperty("db_port");
    }

    public static String getDbDbms() {
        return ConfManager.LazyHolder.INSTANCE.getProperty("db_dbms");
    }

    public static String getDbName() {
        return ConfManager.LazyHolder.INSTANCE.getProperty("db_name");
    }

    public static String getDbUser() {
        return ConfManager.LazyHolder.INSTANCE.getProperty("db_user");
    }

    public static String getDbPassword() {
        return ConfManager.LazyHolder.INSTANCE.getProperty("db_password");
    }

    public static String getAppPortTcp() {
        return ConfManager.LazyHolder.INSTANCE.getProperty("app_port_tcp");
    }

    public static String getAppPortWs() {
        return ConfManager.LazyHolder.INSTANCE.getProperty("app_port_ws");
    }

    public static String getDevBrMaster() {
        return ConfManager.LazyHolder.INSTANCE.getProperty("dev_br_master");
    }

    public static String getDevBrSlave() {
        return ConfManager.LazyHolder.INSTANCE.getProperty("dev_br_slave");
    }

    public static String getDevFprMaster() {
        return ConfManager.LazyHolder.INSTANCE.getProperty("dev_fpr_master");
    }

    public static String getDevFprSlave() {
        return ConfManager.LazyHolder.INSTANCE.getProperty("dev_fpr_slave");
    }

    public static boolean isDevTemperature() {
        return "on".equals(ConfManager.LazyHolder.INSTANCE.getProperty("dev_temperature"));
    }

    public static double getDevTemperatureDelta() {
        String propertyValue = ConfManager.LazyHolder.INSTANCE.getProperty("dev_t_delta");

        try {
            return propertyValue != null && !propertyValue.trim().isEmpty()?Double.parseDouble(propertyValue):-1.0D;
        } catch (NumberFormatException var2) {
            SmartLogger.getLogger().log(Level.SEVERE, "Invalid value for property Temperature Measurement Delta", var2);
            return -1.0D;
        }
    }

    public static int getDevTemperatureDelay() {
        String propertyValue = ConfManager.LazyHolder.INSTANCE.getProperty("dev_t_delay");

        try {
            return propertyValue != null && !propertyValue.trim().isEmpty()?Integer.parseInt(propertyValue):-1;
        } catch (NumberFormatException var2) {
            SmartLogger.getLogger().log(Level.SEVERE, "Invalid value for property Temperature Measurement Delay", var2);
            return -1;
        }
    }

    public static boolean setDbHost(String host) {
        return ConfManager.LazyHolder.INSTANCE.setProperty("db_host", host);
    }

    public static boolean setDbPort(String port) {
        return ConfManager.LazyHolder.INSTANCE.setProperty("db_port", port);
    }

    public static boolean setDbName(String name) {
        return ConfManager.LazyHolder.INSTANCE.setProperty("db_name", name);
    }

    public static boolean setDbUser(String username) {
        return ConfManager.LazyHolder.INSTANCE.setProperty("db_user", username);
    }

    public static boolean setDbPassword(String password) {
        return ConfManager.LazyHolder.INSTANCE.setProperty("db_password", password);
    }

    public static boolean setDbDbms(String dbms) {
        return ConfManager.LazyHolder.INSTANCE.setProperty("db_dbms", dbms);
    }

    public static boolean setDbConfiguration(DbConfiguration dbConfiguration) {
        return setDbHost(dbConfiguration.getHost()) && setDbPort(String.valueOf(dbConfiguration.getPort())) && setDbName(dbConfiguration.getName()) && setDbUser(dbConfiguration.getUser()) && setDbPassword(dbConfiguration.getPassword()) && setDbDbms(dbConfiguration.getDbms());
    }

    public static boolean setProbeConfiguration(Settings settings) {
        return setDevTemperatureDelay(settings.getDelay()) && setDevTemperatureDelta(settings.getDelta()) && setDevTemperature(settings.isEnabled());
    }

    public static boolean setDevBrMaster(String serialPort) {
        return ConfManager.LazyHolder.INSTANCE.setProperty("dev_br_master", serialPort);
    }

    public static boolean setDevBrSlave(String serialPort) {
        return ConfManager.LazyHolder.INSTANCE.setProperty("dev_br_slave", serialPort);
    }

    public static boolean setDevFprMaster(String serialNumber) {
        return ConfManager.LazyHolder.INSTANCE.setProperty("dev_fpr_master", serialNumber);
    }

    public static boolean setDevFprSlave(String serialNumber) {
        return ConfManager.LazyHolder.INSTANCE.setProperty("dev_fpr_slave", serialNumber);
    }

    public static boolean setDevTemperature(boolean state) {
        return ConfManager.LazyHolder.INSTANCE.setProperty("dev_temperature", state?"on":"off");
    }

    public static boolean setDevTemperatureDelay(int seconds) {
        return ConfManager.LazyHolder.INSTANCE.setProperty("dev_t_delay", String.valueOf(seconds));
    }

    public static boolean setDevTemperatureDelta(double delta) {
        return ConfManager.LazyHolder.INSTANCE.setProperty("dev_t_delta", String.valueOf(delta));
    }

    private static class LazyHolder {
        private static final ConfManager INSTANCE = new ConfManager();

        private LazyHolder() {
        }
    }
}
