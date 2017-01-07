In order to have html2pdf printer working you need to install this in IIS using a win32 application pool otherwise you get an error:
<<cannot load win 32 dll etc>>
Also, the default path for this should be http://localhost/html2pdf otherwise please change the WebConfig in TournamentTreeApp to point to the new endpoint