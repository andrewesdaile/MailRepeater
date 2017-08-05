# MailRepeater
#### Easy-to-use email forwarder for servers running IIS6

MailRepeater is a lightweight application that sits on your mail server, takes incoming emails and redirects them to the address or addresses of your choice. It works with the drop and pickup folders of IIS.

The application is especially useful when you are not running Microsoft Exchange, for example your email is hosted elsewhere but you would like to receive copies of all emails sent to your domain. This is a common situation when running your own VPS.

To get started, take the EXE and Config files and put them into a new folder. Now, modify the configuration file to suit the setup on your server. The appSettings section is first:

```
  <appSettings>
    <add key="CheckInterval" value="60" />
    <add key="DropFolder" value="C:\inetpub\mailroot\Drop" />
    <add key="PickupFolder" value="C:\inetpub\mailroot\Pickup" />
    <add key="Sender" value="postoffice@your-domain.com" />
  </appSettings>
```

* CheckInterval - The number of seconds between each check of the drop folder.
* DropFolder - The folder to check for incoming emails. When a new file is detected, it is processed and taken from the folder.
* PickupFolder - The folder where outgoing emails are placed. IIS checks this folder and sends emails from it.
* Sender - The name and/or email address to put on each outgoing email. Note that the reply-to will be set to the original sender, not this address.

Next is the keepHeaderList section. This is where you can tweak the headers that are retained when each email is forwarded on. Generally, you will not need to change any of the key / value pairs in this section, but it's there just in case:

```
  <keepHeaderList>
    <add key="Date" value="" />
    <add key="Subject" value="" />
    <add key="MIME-Version" value="" />
    <add key="Content-Type" value="" />
    <add key="Content-Transfer-Encoding" value="" />
    <add key="Return-Path" value="" />
  </keepHeaderList>
```

Finally, there is the destinationList section. In this section you can define the destination emails for each given source email address. Note that the '\*' wildcard is supported and can catch all emails from a given domain or even catch all emails from every domain.

```
  <destinationList>
    <add key="info@your-domain.com" value="&quot;Person Name&quot; &lt;person.name@gmail.com&gt;" />
    <add key="noreply@your-domain.com" value="" />
    <add key="*@your-domain.com" value="" />
    <add key="*" value="" />
  </destinationList>
```
