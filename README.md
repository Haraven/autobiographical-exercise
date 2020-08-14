# autobiographical-exercise

Basic console-based e-mail reader application, used in conjunction with some [writing guidelines](https://docs.google.com/document/d/18doylw8PJhlrARf5Pvpo_ae5FfNPjlZqvVm_UAUXfQU/edit?usp=sharing) I set up for an autobiographical exercise at my university.

## Autobiographies
Students will be writing anonymous autobiographies, which they will then attach to messages (as pdfs or some other format), sent with a specified keyword (i.e. 'autobiografie') in the title, to the application email. 
The application will retrieve the autobiographies regularly and will send the new ones over to random *registered* email addresses for reading and receiving feedback.
Each autobiography will only be sent once and to an email which hasn't received an autobiography already. 
Once an author has sent his work, subsequent autobiographies will be ignored from him or her.

## Feedback for autobiographies
On the other end of things, registered emails will be receiving random, anonymous autobiographies that they have to read and then send feedback for, using the guidelines mentioned earlier.
Once a user has written his or her feedback, it will be sent to the application email with a specified keyword (i.e. 'feedback') in the title.
The application will check which autobiography author the feedback is paired to and will then send the feedback file to the author for his or her reading.

## Safeties
There are validations and safety checks and logs done at just about every step of the way, so there is in theory no way to lose the data even if the application crashes, and debugging (should it be needed) should be more than easy to perform. Additionally, the received emails will never be deleted anyway, so the documents themselves won't be lost. The downloaded attachments will however be deleted each time the application is launched, as they will have either been already sent out or they will be redownloaded to be sent out again anyway.

## Adapting for your own usage
- You will need to set up Gmail API credentials (see [this](https://developers.google.com/gmail/api/quickstart/dotnet)) first. Download the `credentials.json` file from the Cloud Platform project and put it under `AutobiographyManager/Config/credentials.json`. As specified in the reference, the file must **always** be copied to the output directory (click on file in _Solution Explorer -> Properties -> Copy to Output Directory_: set to `Copy always`)
- You will need to change the application email under App.config.
- You will need to create a `AutobiographyManager/Data/registered-users.json` file, which should contain a JSON list of strings denoting email addresses which can send and receive autobiographies and feedback. This file needs to always be copied to the output directory as well (see above).

If you've set it up correctly, this is roughly what you'll be seeing in the console:
![](https://i.imgur.com/AoFqTY1.png)

Any other changes you want to make will imply changes to the code, which is up to you to explore.
