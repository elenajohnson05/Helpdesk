# Helpdesk
Smart Helpdesk Ticketing System

To enable users to submit tickets, we have a Model-Driven PowerApp which uses a Ticket table in Dataverse. Our entire Dataverse
schema consists of tables Ticket, User, Agent, Category, and Status. Users create a ticket, which is then assigned an Agent who specializes in a specific Category. Our AgentAssignment flow triggers an Azure Function upon creation of a new Ticket (or modification of the Description field), which dynamically assigns an Agent using OpenAI based upon the Ticket Description. A seperate flow sends a comfirmation email upon Ticket creation. We can also view a graph of Tickets grouped by Category in our Model-Driven App Chart.
