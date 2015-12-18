package com.spacecode.smartserver.command;


public class ClientCommandException extends Exception {

   public ClientCommandException(String errorMessage) {
      super(errorMessage);
   }
}
