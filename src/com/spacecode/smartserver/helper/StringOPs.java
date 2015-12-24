package com.spacecode.smartserver.helper;

import java.util.Collection;

/**
 * Created by MY on 24/10/2015.
 */
public final class StringOPs {
    public static String getCommaSaparetedString(Collection<String> allTagsnew){
        StringBuilder commaSepValueBuilder = new StringBuilder();
        for ( int i = 0; i< allTagsnew.size(); i++) {
            //append the value into the builder ....
            commaSepValueBuilder.append(allTagsnew.toArray()[i]);
            if (i != allTagsnew.size() - 1) {
                commaSepValueBuilder.append(",");
            }
        }
        return commaSepValueBuilder.toString();
    }
}
