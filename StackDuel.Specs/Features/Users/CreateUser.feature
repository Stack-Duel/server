# Issue: #<fill in the GitHub issue/PBI number this feature file was written for>
Feature: Create user
    As a new player
    I want to create an account
    So that I can start playing Stack Duel

    Scenario: Successful account creation
        Given no user exists with sub "auth0|new-player"
        When I create a user with username "player_one" and sub "auth0|new-player"
        Then the user is created successfully

    Scenario: Username is required
        Given no user exists with sub "auth0|new-player"
        When I create a user with username "" and sub "auth0|new-player"
        Then the request fails with an error containing "Username"

    Scenario: Username exceeds the maximum length
        Given no user exists with sub "auth0|new-player"
        When I create a user with username "this-username-is-way-too-long-to-be-valid" and sub "auth0|new-player"
        Then the request fails with an error containing "Username"

    Scenario: Sub must be unique
        Given a user already exists with sub "auth0|existing-player"
        When I create a user with username "player_two" and sub "auth0|existing-player"
        Then the request fails with an error containing "already exists"
