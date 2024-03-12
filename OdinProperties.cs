using System;

#if !ODIN_INSPECTOR
public class ShowIf : Attribute
{
    public ShowIf(string condition, bool animate = true) {
    
    }
}
public class HideIf : Attribute
{
    public HideIf(string condition, bool animate = true) {
    
    }
}
#endif