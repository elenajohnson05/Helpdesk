# Ticket Confirmation Flow
The flow is triggered upon creation of a new row in the Ticket table of Dataverse.
The 'Get a Row by ID' action then retrieves the information of the User who created the ticket.
Using the Primary Email field of the owning User, we then send an email (including dynamic text of the User's Name and the Subject of the Ticket in the body)
confirming that the Helpdesk has recieved their Ticket.

# Agent Assignment Flow
The flow is triggered upon creation of a new row or modification of the Description field
in the Ticket table of dataverse.
The HTTP action invokes our Agent Assignment Azure Function, dynamically assigning an Agent and filling the Category field based on
the ticket description.
