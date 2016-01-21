using System;
using System.Collections;


class RfidData
{
    public ArrayList allTags;
    public ArrayList added;
    public ArrayList removed;
    public ArrayList present;
    public String msg;
    public String errorMsg;

    public RfidData()
    { }

    public RfidData(ArrayList all, ArrayList added, ArrayList removed, ArrayList present, String msg, String errorMsg)
    {
        this.allTags = all;
        this.added = added;
        this.removed = removed;
        this.present = present;
        this.msg = msg;
        this.errorMsg = errorMsg;
    }
     
}