CREATE TABLE Users
(
	[User_ID] [int] IDENTITY PRIMARY KEY NOT NULL,
	[Name] [varchar](50) NOT NULL,
	[Department] [varchar](50),
	[Address] [varchar](50) NULL,
	[Email] [varchar](50) UNIQUE NOT NULL,
	[Phone] [varchar](50) NULL,
);
CREATE TABLE Categories
(
	[Category_ID] [int] IDENTITY PRIMARY KEY NOT NULL,
	[CategoryName] [varchar](50) UNIQUE NOT NULL
);

CREATE TABLE Agents
(
	[Agent_ID] [int] IDENTITY PRIMARY KEY NOT NULL,
	[Name] [varchar](50) NOT NULL,
	[Email] [varchar](50) UNIQUE NOT NULL,
	[SpecialtyCategory_ID] [int] NULL,
	[IsAvailable] [bit] DEFAULT 1,
	FOREIGN KEY (SpecialtyCategory_ID) REFERENCES Categories(Category_ID)
);
CREATE TABLE Statuses
(
	[Status_ID] [int] PRIMARY KEY NOT NULL,
	[Status] [varchar](20) UNIQUE NOT NULL
);
CREATE TABLE Tickets
(
	[Ticket_ID] [int] PRIMARY KEY NOT NULL,
	[User_ID] [int] NOT NULL,
	[Agent_ID] [int] NULL,
	[Category_ID] [int] NOT NULL,
	[Status_ID] [int] NOT NULL,
	[Subject] [varchar](200) NOT NULL,
	[Description] [varchar](MAX),
	[Priority] [varchar](20) CHECK (Priority IN ('Low', 'Medium', 'High')) DEFAULT 'Medium',
	FOREIGN KEY (User_ID) REFERENCES Users(User_ID),
    FOREIGN KEY (Agent_ID) REFERENCES Agents(Agent_ID),
    FOREIGN KEY (Category_ID) REFERENCES Categories(Category_ID),
    FOREIGN KEY (Status_ID) REFERENCES Statuses(Status_ID)
);
