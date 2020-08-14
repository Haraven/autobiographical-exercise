# autobiographical-exercise

Basic console-based e-mail reader application, used in conjunction with some writing guidelines I set up for an autobiographical exercise at my university.

## Autobiographies
Students will be writing anonymous autobiographies, which they will then attach to messages (as pdfs or some other format), sent with a specified keyword in the title, to the application email. 
The application will retrieve the autobiographies regularly and will send the new ones over to random *registered* email addresses for reading and receiving feedback.
Each autobiography will only be sent once and to an email which hasn't received an autobiography already. 
Once an author has sent his work, subsequent autobiographies will be ignored from him or her.

## Feedback for autobiographies
On the other end of things, registered emails will be receiving random, anonymous autobiographies that they have to read and then send feedback for, using the guidelines mentioned earlier.
Once a user has written his or her feedback, it will be sent to the application email with a specified keyword in the title.
The application will check which autobiography author the feedback is paired to and will then send the feedback file to the author for his or her reading.

## Safeties
There are validations and safety checks and logs done at just about every step of the way, so there is in theory no way to lose the data even if the application crashes, and debugging (should it be needed) should be more than easy to perform. Additionally, the received emails will never be deleted anyway, so even if the downloaded attachments and autobiography-feedback pairings are deleted, the documents themselves won't be lost.
