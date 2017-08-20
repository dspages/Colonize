# Colonize

Colonize is a turn-based strategy game written in C# using the Unity game engine. In the beginning of the game the player has a crashed spaceship and three colonists.

![image](https://github.com/dspages/Colonize/blob/master/Screenshot%202014-07-19%2020.59.31.png)

The game logic is handled by C# scripts, which reads the game rules from txt files that can be easily modified and govern game rules. For instance, jobtypes.txt contains the logic for the 25 different types of job a colonist can do. Most of these require a particular technology research to unlock. Examples of jobs include constructing a new building, researching a new technology, gathering a resource, and fighting an enemy. Another txt file controlling game rules is techtree.txt, which contains a rich interconnecting tech tree with 53 different technologies, each of which unlocks a new job, building, equipment, or other power. Events.txt contains various random events that can occur. Dozens of different types of buildings can be built, each with a unique function and role. For instance, the workshop allows the construction of new gear your colonists can equip to be better able to fight off hostile creatures or better at a given job. The destasis chamber allows you to rescue people from stasis in the crashed spaceship. And the Archeaological camp lets you excavate alien ruins to access unique items, technologies, and events.

Colonists that complete jobs successfully can gain experience and the player can train up their skills to make each colonist specialized at a particular job. New colonists can be born or alien species can be recruited to join the colony as a result of certain decisions in random events. Hostile aliens can be converted to join the colony by a colonist using the propoganda skill. Or they can be fought off using weapons skills.

All this together leads to a compelling experience and hours of entertainment. Below is a screenshot of a mid-game colony with a variety of constructed buildings, as well as friendly and enemy people.

![image](https://github.com/dspages/Colonize/blob/master/screenshot%202014-09-08.png)
