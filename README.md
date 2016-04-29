# XliffResourceExcluder

Ever tried to integrate the Multilingual App Toolkit into existing projects, like ASP.NET with Entity Framework and Code First migrations, or Windows Forms with lots of resource (resx) files, where some resource files should never be translated?
If so, you have probably noticed that the generated XLIFF files for translation gets bloated with unwanted resources that you never want to translate.

With the XLIFF Resource Excluder you can batch select XLIFF files to process and resource (resx, resw) files, or even non-string types altogether, to exclude from ever being translated.  
