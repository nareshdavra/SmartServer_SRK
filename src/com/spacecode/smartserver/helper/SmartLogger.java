//
// Source code recreated from a .class file by IntelliJ IDEA
// (powered by Fernflower decompiler)
//

package com.spacecode.smartserver.helper;

import com.spacecode.smartserver.SmartServer;
import java.io.IOException;
import java.text.DateFormat;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.MissingResourceException;
import java.util.logging.ConsoleHandler;
import java.util.logging.FileHandler;
import java.util.logging.Formatter;
import java.util.logging.Level;
import java.util.logging.LogManager;
import java.util.logging.LogRecord;
import java.util.logging.Logger;

public final class SmartLogger extends Logger {
    private static final String LOG_FILENAME = "smartserver.log";
    private static final String LOG_FILE = SmartServer.getWorkingDirectory()+ "/"+LOG_FILENAME; //SmartServer.getWorkingDirectory() +
    private static final SmartLogger LOGGER = new SmartLogger();

    protected SmartLogger() throws MissingResourceException {
        super("SmartLogger", (String)null);
    }

    public static void initialize() {
        try {
            LogManager.getLogManager().reset();
            SmartLogger.SmartConsoleHandler e = new SmartLogger.SmartConsoleHandler();
            FileHandler fileHandler = new FileHandler(LOG_FILE, true);
            SmartLogger.ShortFormatter formatter = new SmartLogger.ShortFormatter();
            fileHandler.setLevel(Level.FINE);
            fileHandler.setFormatter(formatter);
            e.setLevel(Level.INFO);
            e.setFormatter(formatter);
            LOGGER.addHandler(fileHandler);
            LOGGER.addHandler(e);
        } catch (SecurityException | IOException var3) {
            LOGGER.log(Level.SEVERE, "Unable to initialize SmartLogger.", var3);
        }

    }

    public static SmartLogger getLogger() {
        return LOGGER;
    }

    static class SmartConsoleHandler extends ConsoleHandler {
        public SmartConsoleHandler() {
            super.setOutputStream(System.out);
        }
    }

    static class ShortFormatter extends Formatter {
        private final DateFormat df = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss");

        ShortFormatter() {
        }

        public String format(LogRecord record) {
            StringBuilder builder = new StringBuilder();
            builder.append(this.df.format(new Date(record.getMillis()))).append(" ");
            builder.append("[").append(record.getLevel()).append("] ");
            builder.append(this.formatMessage(record));
            builder.append("\n");
            if(record.getThrown() != null) {
                builder.append(record.getThrown().getMessage());
                builder.append("\n");
            }

            return builder.toString();
        }
    }
}
