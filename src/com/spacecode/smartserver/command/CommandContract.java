package com.spacecode.smartserver.command;

import java.lang.annotation.ElementType;
import java.lang.annotation.Inherited;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

@Retention(RetentionPolicy.RUNTIME)
@Target({ElementType.TYPE})
@Inherited
@interface CommandContract {

   int paramCount() default 0;

   boolean strictCount() default false;

   boolean deviceRequired() default false;

   boolean adminRequired() default false;

   String responseIfInvalid() default "false";

   boolean noResponseWhenInvalid() default false;

   boolean respondToAllIfInvalid() default false;
}
