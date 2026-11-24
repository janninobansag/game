# VAREN Documentation

This folder contains the project documentation.

- [Project details and change log](DETAILS.md)
- [Business Model Canvas](BUSINESS_MODEL_CANVAS.md)
- [Business Roadmap](BUSINESS_ROADMAP.md)
- [Conceptual framework](CONCEPTUAL_FRAMEWORK.md)
- [System data dictionary](DATA_DICTIONARY.md)
- [Network design](NETWORK_DESIGN.md)
- [Database guide](DATABASE_README.md)
- [Game story](GAME%20STORY.md)
- [Player guide: Linux and macOS](PLAYING_ON_LINUX_AND_MAC.md)
- [Regression test cases for the testing plan](TESTING_REGRESSIONS.md)

## Database ERDs (schema 11)

- [Chapter 1 / Normal ERD](CHAPTER_1_DATABASE_ERD.dbml)
- [Chapter 2 / Hard ERD](CHAPTER_2_DATABASE_ERD.dbml)
- [Global settings ERD](SETTINGS_DATABASE_ERD.dbml)
- [Relationship explanations](DATABASE_LOGICAL_RELATIONSHIPS.md)

Paste each chapter's DBML into dbdiagram.io to export the diagram for the VAREN manuscript. Both chapter ERDs include banana health-reward persistence. `BananaFruitData` has no position/rotation columns; those belong to `DroppedItemData`. `BananaFruitId` is an application-level link, while `SaveProfileId` is an enforced SQLite foreign key.

The [item-based health reward implementation and suggested IODRF remarks](DETAILS.md#oral-defense-recommendation-item-based-health-rewards) describe the banana feature.
