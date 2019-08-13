# Akzin.Crm.DataMigrator
Data Migration for Dynamics CRM 2016 and 365


# Usage


## Export data
Akzin.Crm.DataMigrator export -c AuthType=Office365;Url=http://contoso:8080/Test;UserName=jsmith@contoso.onmicrosoft.com;Password=passcode -e contact,account -d c:\Temp

## Import data
Akzin.Crm.DataMigrator import -c AuthType=Office365;Url=http://contoso:8080/Test;UserName=jsmith@contoso.onmicrosoft.com;Password=passcode -e contact,account -d c:\Temp


