## <small>0.2.28 (2022-02-15)</small>

* Add extra delete and some comments, also fetch better the last blolck query from memory not disk (#1 ([37a33c8](https://github.com/block-core/blockcore-indexer/commit/37a33c8)), closes [#106](https://github.com/block-core/blockcore-indexer/issues/106)
* Changed the logic back to task.run for the transaction table because … (#103) ([4e2c281](https://github.com/block-core/blockcore-indexer/commit/4e2c281)), closes [#103](https://github.com/block-core/blockcore-indexer/issues/103)
* Update changelog and version ([fdc3374](https://github.com/block-core/blockcore-indexer/commit/fdc3374))



## <small>0.2.27 (2022-02-13)</small>

* Add some guards and checks on the utxo table (#98) ([8b445da](https://github.com/block-core/blockcore-indexer/commit/8b445da)), closes [#98](https://github.com/block-core/blockcore-indexer/issues/98)
* Breaking change version upgrade ([f73be31](https://github.com/block-core/blockcore-indexer/commit/f73be31))
* Fix issue with paging on address transactions (#97) ([e31b64d](https://github.com/block-core/blockcore-indexer/commit/e31b64d)), closes [#97](https://github.com/block-core/blockcore-indexer/issues/97) [#96](https://github.com/block-core/blockcore-indexer/issues/96)
* Update changelog and version ([48081e2](https://github.com/block-core/blockcore-indexer/commit/48081e2))



## <small>0.1.26 (2022-02-12)</small>

* Changed from hash to object id (#94) ([8bbe293](https://github.com/block-core/blockcore-indexer/commit/8bbe293)), closes [#94](https://github.com/block-core/blockcore-indexer/issues/94)
* Update changelog and version ([e5a9045](https://github.com/block-core/blockcore-indexer/commit/e5a9045))



## <small>0.1.25 (2022-02-12)</small>

* Add some notes regarding running with dotnet ([d63a281](https://github.com/block-core/blockcore-indexer/commit/d63a281))
* Added table for reorg blocks (#93) ([607ac0a](https://github.com/block-core/blockcore-indexer/commit/607ac0a)), closes [#93](https://github.com/block-core/blockcore-indexer/issues/93)
* fix cirrus test config file ([7731ae9](https://github.com/block-core/blockcore-indexer/commit/7731ae9))
* Update changelog and version ([9f29234](https://github.com/block-core/blockcore-indexer/commit/9f29234))



## <small>0.1.24 (2022-02-09)</small>

* Add Cybits network package to indexer ([0acce7c](https://github.com/block-core/blockcore-indexer/commit/0acce7c))
* Added guard for empty results in the rewind lookup (#89) ([f5103fd](https://github.com/block-core/blockcore-indexer/commit/f5103fd)), closes [#89](https://github.com/block-core/blockcore-indexer/issues/89)
* Move the StoreRawTransactions to development config ([d9fa519](https://github.com/block-core/blockcore-indexer/commit/d9fa519))
* Performance enhances (#88) ([91f0572](https://github.com/block-core/blockcore-indexer/commit/91f0572)), closes [#88](https://github.com/block-core/blockcore-indexer/issues/88)
* Thorw if cirrus api failed to fetch a recipt ([cbf28c9](https://github.com/block-core/blockcore-indexer/commit/cbf28c9))
* Update changelog and version ([eb94c1f](https://github.com/block-core/blockcore-indexer/commit/eb94c1f))



## <small>0.1.23 (2022-02-06)</small>

* Add ability to manually trigger build ([3c95378](https://github.com/block-core/blockcore-indexer/commit/3c95378))
* Adding unconfirmed trx to history (#87) ([367df2c](https://github.com/block-core/blockcore-indexer/commit/367df2c)), closes [#87](https://github.com/block-core/blockcore-indexer/issues/87)
* Fix bug on client when fullnode return null ([36489c7](https://github.com/block-core/blockcore-indexer/commit/36489c7))
* show indexing on progress bar (#86) ([2b4f1a5](https://github.com/block-core/blockcore-indexer/commit/2b4f1a5)), closes [#86](https://github.com/block-core/blockcore-indexer/issues/86)
* Update CHANGELOG.md ([9a00b36](https://github.com/block-core/blockcore-indexer/commit/9a00b36))
* Update version ([579dbb4](https://github.com/block-core/blockcore-indexer/commit/579dbb4))



## <small>0.1.22 (2022-01-30)</small>

* Add a basic docker-compose to run local test database ([3a8e9b3](https://github.com/block-core/blockcore-indexer/commit/3a8e9b3))
* add api endpoint for fetching trx hex ([9765dde](https://github.com/block-core/blockcore-indexer/commit/9765dde))
* Add instructions on starting the MongoDB container ([362a3de](https://github.com/block-core/blockcore-indexer/commit/362a3de))
* Added Utxo table (#58) ([d7411ab](https://github.com/block-core/blockcore-indexer/commit/d7411ab)), closes [#58](https://github.com/block-core/blockcore-indexer/issues/58)
* Check indexes on startup (#74) ([6e2b2b7](https://github.com/block-core/blockcore-indexer/commit/6e2b2b7)), closes [#74](https://github.com/block-core/blockcore-indexer/issues/74)
* Closes #60 (#61) ([a8ac2bb](https://github.com/block-core/blockcore-indexer/commit/a8ac2bb)), closes [#60](https://github.com/block-core/blockcore-indexer/issues/60) [#61](https://github.com/block-core/blockcore-indexer/issues/61)
* Closes #64 (#73) ([cdc8970](https://github.com/block-core/blockcore-indexer/commit/cdc8970)), closes [#64](https://github.com/block-core/blockcore-indexer/issues/64) [#73](https://github.com/block-core/blockcore-indexer/issues/73)
* Contract code table (#57) ([1ba3444](https://github.com/block-core/blockcore-indexer/commit/1ba3444)), closes [#57](https://github.com/block-core/blockcore-indexer/issues/57)
* Fix block index count ([fa4b9ba](https://github.com/block-core/blockcore-indexer/commit/fa4b9ba))
* fix minor utxo bug ([aba62c1](https://github.com/block-core/blockcore-indexer/commit/aba62c1))
* Pulled lookup result to the indexer and than update mongo and replaced the fluent c# with bson docum ([931a364](https://github.com/block-core/blockcore-indexer/commit/931a364)), closes [#82](https://github.com/block-core/blockcore-indexer/issues/82)
* Removed computed utxo table and use the unspent output table instead (#76) ([5797a76](https://github.com/block-core/blockcore-indexer/commit/5797a76)), closes [#76](https://github.com/block-core/blockcore-indexer/issues/76) [#67](https://github.com/block-core/blockcore-indexer/issues/67)
* return tx size and fee (#84) ([f13ca07](https://github.com/block-core/blockcore-indexer/commit/f13ca07)), closes [#84](https://github.com/block-core/blockcore-indexer/issues/84)
* Richlist to use new utxo table (#75) ([70a319a](https://github.com/block-core/blockcore-indexer/commit/70a319a)), closes [#75](https://github.com/block-core/blockcore-indexer/issues/75) [#66](https://github.com/block-core/blockcore-indexer/issues/66)
* Upgrade version prepare for release ([665beea](https://github.com/block-core/blockcore-indexer/commit/665beea))
* Use specific container image we have verified with ([d924e6b](https://github.com/block-core/blockcore-indexer/commit/d924e6b))



## <small>0.1.21 (2022-01-14)</small>

* Create api endpoints for smart contracts (#55) ([8d7836f](https://github.com/block-core/blockcore-indexer/commit/8d7836f)), closes [#55](https://github.com/block-core/blockcore-indexer/issues/55)
* Fix bug with amount of mined pr. address ([0745ace](https://github.com/block-core/blockcore-indexer/commit/0745ace)), closes [#54](https://github.com/block-core/blockcore-indexer/issues/54)
* Update version ([6f7c535](https://github.com/block-core/blockcore-indexer/commit/6f7c535))



## <small>0.1.20 (2022-01-12)</small>

* Prepare next release of Blockcore Indexer ([6b515b4](https://github.com/block-core/blockcore-indexer/commit/6b515b4))



## <small>0.1.19 (2022-01-11)</small>

* Add cirrus storage operations (#49) ([7d48458](https://github.com/block-core/blockcore-indexer/commit/7d48458)), closes [#49](https://github.com/block-core/blockcore-indexer/issues/49)
* Add SBC and RSC networks and launch settings ([72ccbec](https://github.com/block-core/blockcore-indexer/commit/72ccbec))
* Adjusted namespaces with Rider refactoring (#48) ([c85c4b9](https://github.com/block-core/blockcore-indexer/commit/c85c4b9)), closes [#48](https://github.com/block-core/blockcore-indexer/issues/48)
* Changed the index creation to hash the indexes of Outpoints (#51) ([e7c5d44](https://github.com/block-core/blockcore-indexer/commit/e7c5d44)), closes [#51](https://github.com/block-core/blockcore-indexer/issues/51)
* Cirrus new nodes api  (#53) ([b0ae389](https://github.com/block-core/blockcore-indexer/commit/b0ae389)), closes [#53](https://github.com/block-core/blockcore-indexer/issues/53)
* Moved the bitcoin client into an interface (#50) ([a683c0e](https://github.com/block-core/blockcore-indexer/commit/a683c0e)), closes [#50](https://github.com/block-core/blockcore-indexer/issues/50)
* Specify branch for action ([1943148](https://github.com/block-core/blockcore-indexer/commit/1943148))
* Update pipelines for indexer to install .NET 6 ([a2e94b0](https://github.com/block-core/blockcore-indexer/commit/a2e94b0))
* Upgrades indexer to .NET 6. (#52) ([a116d44](https://github.com/block-core/blockcore-indexer/commit/a116d44)), closes [#52](https://github.com/block-core/blockcore-indexer/issues/52)



## <small>0.1.18 (2022-01-05)</small>

* Fix the docker image for Cirrus Indexer ([9daccf7](https://github.com/block-core/blockcore-indexer/commit/9daccf7))



## <small>0.1.17 (2022-01-05)</small>

* Update with correct namespaces ([fa1147c](https://github.com/block-core/blockcore-indexer/commit/fa1147c))



## <small>0.1.16 (2022-01-05)</small>

* Add a todo for mempool entries in to in history ([48fd2b1](https://github.com/block-core/blockcore-indexer/commit/48fd2b1))
* Add docker setup for Cirrus indexer (#46) ([9ac7052](https://github.com/block-core/blockcore-indexer/commit/9ac7052)), closes [#46](https://github.com/block-core/blockcore-indexer/issues/46)
* Add more startup profiles for regtest and main ([362fee3](https://github.com/block-core/blockcore-indexer/commit/362fee3))
* Add test network configuration ([954d07e](https://github.com/block-core/blockcore-indexer/commit/954d07e))
* Added cirrus project with migration of missing consensus factory code (#42) ([326165d](https://github.com/block-core/blockcore-indexer/commit/326165d)), closes [#42](https://github.com/block-core/blockcore-indexer/issues/42)
* Address computation for faster fetching of address balance  (#29) ([6ecbd9a](https://github.com/block-core/blockcore-indexer/commit/6ecbd9a)), closes [#29](https://github.com/block-core/blockcore-indexer/issues/29)
* Clean code (#36) ([83338e5](https://github.com/block-core/blockcore-indexer/commit/83338e5)), closes [#36](https://github.com/block-core/blockcore-indexer/issues/36)
* Cleaning up some code ([d4a2067](https://github.com/block-core/blockcore-indexer/commit/d4a2067))
* Default to CRS for Cirrus indexer when no chain is specified ([26cde7c](https://github.com/block-core/blockcore-indexer/commit/26cde7c))
* Enforce OCC on computed addresses table (still not perfect) ([ede2d5e](https://github.com/block-core/blockcore-indexer/commit/ede2d5e))
* Feature/indexer core (#47) ([b1644c1](https://github.com/block-core/blockcore-indexer/commit/b1644c1)), closes [#47](https://github.com/block-core/blockcore-indexer/issues/47)
* Filter out controller not part of indexer ([fdbbc7c](https://github.com/block-core/blockcore-indexer/commit/fdbbc7c))
* Fix bug in computed address tables ([08c2c5d](https://github.com/block-core/blockcore-indexer/commit/08c2c5d))
* Fix for Linux build ([473173a](https://github.com/block-core/blockcore-indexer/commit/473173a))
* Fix logs ([9411deb](https://github.com/block-core/blockcore-indexer/commit/9411deb))
* Fix reorg bug ([5de6c1f](https://github.com/block-core/blockcore-indexer/commit/5de6c1f))
* Improve block fetching ([fcdc4e1](https://github.com/block-core/blockcore-indexer/commit/fcdc4e1))
* Mempool in store (#35) ([17e9131](https://github.com/block-core/blockcore-indexer/commit/17e9131)), closes [#35](https://github.com/block-core/blockcore-indexer/issues/35)
* move the script parser to an interface (#43) ([367ca13](https://github.com/block-core/blockcore-indexer/commit/367ca13)), closes [#43](https://github.com/block-core/blockcore-indexer/issues/43)
* New db schema with delayed indexing and inserts only for initial sync (#34) ([abb98dd](https://github.com/block-core/blockcore-indexer/commit/abb98dd)), closes [#34](https://github.com/block-core/blockcore-indexer/issues/34)
* Remove StartBlockIndex property (#33) ([58fe5fa](https://github.com/block-core/blockcore-indexer/commit/58fe5fa)), closes [#33](https://github.com/block-core/blockcore-indexer/issues/33)
* Rename tables (#39) ([06cb253](https://github.com/block-core/blockcore-indexer/commit/06cb253)), closes [#39](https://github.com/block-core/blockcore-indexer/issues/39)
* Rename the Cirrus indexer project ([2f33b29](https://github.com/block-core/blockcore-indexer/commit/2f33b29))
* Set the correct symbol in configuration ([be50f78](https://github.com/block-core/blockcore-indexer/commit/be50f78))
* Streamline indexing task (#25) ([06bc422](https://github.com/block-core/blockcore-indexer/commit/06bc422)), closes [#25](https://github.com/block-core/blockcore-indexer/issues/25)
* take start block index from config ([27d9c85](https://github.com/block-core/blockcore-indexer/commit/27d9c85))
* Update dbschema.md (#40) ([a19a6e4](https://github.com/block-core/blockcore-indexer/commit/a19a6e4)), closes [#40](https://github.com/block-core/blockcore-indexer/issues/40)
* Utxo query method on api (#37) ([3302f9c](https://github.com/block-core/blockcore-indexer/commit/3302f9c)), closes [#37](https://github.com/block-core/blockcore-indexer/issues/37)
* Utxo to computed tables (#45) ([6f6a98d](https://github.com/block-core/blockcore-indexer/commit/6f6a98d)), closes [#45](https://github.com/block-core/blockcore-indexer/issues/45)



## <small>0.1.15 (2021-10-06)</small>

* Add support for Strax ([7022220](https://github.com/block-core/blockcore-indexer/commit/7022220))



## <small>0.1.14 (2021-04-25)</small>

* Add the SERF network ([48f730e](https://github.com/block-core/blockcore-indexer/commit/48f730e))



## <small>0.1.13 (2021-03-09)</small>

* .13 was not released yet ([3e764d4](https://github.com/block-core/blockcore-indexer/commit/3e764d4))
* Add filtering on "Fund", "Locked" and "Burn" ([ca6e076](https://github.com/block-core/blockcore-indexer/commit/ca6e076))
* Add the latest network definitions to Indexer ([6b5738c](https://github.com/block-core/blockcore-indexer/commit/6b5738c))
* Minor fix for reward calculation ([8e50a30](https://github.com/block-core/blockcore-indexer/commit/8e50a30))
* Prepare next release of Indexer ([6bda916](https://github.com/block-core/blockcore-indexer/commit/6bda916))



## <small>0.1.12 (2021-02-12)</small>

* Ensure that all tags are published to Docker Hub ([40d35a0](https://github.com/block-core/blockcore-indexer/commit/40d35a0))
* Re-order API methods ([54baad0](https://github.com/block-core/blockcore-indexer/commit/54baad0))



## <small>0.1.11 (2021-02-12)</small>

* Add Insight API ([5ed039a](https://github.com/block-core/blockcore-indexer/commit/5ed039a))
* Update to release 11 ([4296a55](https://github.com/block-core/blockcore-indexer/commit/4296a55))
* Upgrade packages ([8a35da0](https://github.com/block-core/blockcore-indexer/commit/8a35da0))



## <small>0.1.10 (2021-02-01)</small>

* Fix Linux path for action script ([41d41c5](https://github.com/block-core/blockcore-indexer/commit/41d41c5))
* Update GitHub Actions ([bb46618](https://github.com/block-core/blockcore-indexer/commit/bb46618))
* Update Indexer with latest Blockcore packages ([765a483](https://github.com/block-core/blockcore-indexer/commit/765a483))



## <small>0.1.9 (2020-09-17)</small>

* Add OpenExo and Rutanio network packages to indexer ([d99774a](https://github.com/block-core/blockcore-indexer/commit/d99774a))



## <small>0.1.8 (2020-09-06)</small>

* Add index on the rich list ([58c46b0](https://github.com/block-core/blockcore-indexer/commit/58c46b0))



## <small>0.1.7 (2020-06-23)</small>

* x42 was added, removed and now added again ([8ea25f3](https://github.com/block-core/blockcore-indexer/commit/8ea25f3))



## <small>0.1.6 (2020-06-23)</small>

* Add x42 to the indexer networks ([44deb67](https://github.com/block-core/blockcore-indexer/commit/44deb67))



## <small>0.1.5 (2020-06-22)</small>

* Increase the max length for address from 54 to 100. ([e941009](https://github.com/block-core/blockcore-indexer/commit/e941009))
* Update version and prepare for new release ([0d77e12](https://github.com/block-core/blockcore-indexer/commit/0d77e12))



## <small>0.1.4 (2020-06-04)</small>

* Add easy way to run local indexer settings ([6f1f413](https://github.com/block-core/blockcore-indexer/commit/6f1f413))
* fix mempool trx and add received by address (#18) ([63610c4](https://github.com/block-core/blockcore-indexer/commit/63610c4)), closes [#18](https://github.com/block-core/blockcore-indexer/issues/18)



## <small>0.1.3 (2020-05-11)</small>

* Add debug configuration for Visual Studio Code ([7796d55](https://github.com/block-core/blockcore-indexer/commit/7796d55))
* Add header to README for Indexer ([e7eba60](https://github.com/block-core/blockcore-indexer/commit/e7eba60))
* Add unit test for paging ([67e5399](https://github.com/block-core/blockcore-indexer/commit/67e5399)), closes [#16](https://github.com/block-core/blockcore-indexer/issues/16)
* Added Richlist to Indexer (#14) ([6bbf9e7](https://github.com/block-core/blockcore-indexer/commit/6bbf9e7)), closes [#14](https://github.com/block-core/blockcore-indexer/issues/14)
* Fix issue with indexer not starting when database was empty ([16d1730](https://github.com/block-core/blockcore-indexer/commit/16d1730))
* Update changelog and prepare release ([f9d58ac](https://github.com/block-core/blockcore-indexer/commit/f9d58ac))



## <small>0.1.2 (2020-05-04)</small>

* Add response compression ([3b7247c](https://github.com/block-core/blockcore-indexer/commit/3b7247c)), closes [#13](https://github.com/block-core/blockcore-indexer/issues/13)
* Clean up package-lock.json ([e4ceabb](https://github.com/block-core/blockcore-indexer/commit/e4ceabb))
* Update CHANGELOG ([c37dfb4](https://github.com/block-core/blockcore-indexer/commit/c37dfb4))



## <small>0.1.1 (2020-05-03)</small>

* Increase the address max length input validation to 54 characters. ([69d9bc6](https://github.com/block-core/blockcore-indexer/commit/69d9bc6))



## 0.1.0 (2020-05-03)

* Add GenesisDate to info output ([11d2968](https://github.com/block-core/blockcore-indexer/commit/11d2968))
* Add network information to stats/info and history of known peers ([644bc2c](https://github.com/block-core/blockcore-indexer/commit/644bc2c))
* Configuration load improvements ([8df724f](https://github.com/block-core/blockcore-indexer/commit/8df724f))
* Feature/network support for indexer (#10) ([b9d996b](https://github.com/block-core/blockcore-indexer/commit/b9d996b)), closes [#10](https://github.com/block-core/blockcore-indexer/issues/10)
* Make default release draft non-prerelease ([715ea46](https://github.com/block-core/blockcore-indexer/commit/715ea46))
* Update instructions for debugging the Blockcore Indexer ([8c0ac48](https://github.com/block-core/blockcore-indexer/commit/8c0ac48))



## <small>0.0.5 (2020-04-22)</small>

* Add network packages for BTC and STRAT ([a351754](https://github.com/block-core/blockcore-indexer/commit/a351754))



## <small>0.0.4 (2020-04-20)</small>

* Migrate Blockcore Explorer to Blockcore.NBitcoin ([6227c7c](https://github.com/block-core/blockcore-indexer/commit/6227c7c))



## <small>0.0.3 (2020-04-15)</small>

* Add support for sender address and fix time on transaction ([add700c](https://github.com/block-core/blockcore-indexer/commit/add700c))
* Fix badge table ([2c2093f](https://github.com/block-core/blockcore-indexer/commit/2c2093f))
* Update badge status ([edf66bc](https://github.com/block-core/blockcore-indexer/commit/edf66bc))



## <small>0.0.2 (2020-02-03)</small>

* Update configuration URL ([a632872](https://github.com/block-core/blockcore-indexer/commit/a632872))



## <small>0.0.1 (2020-02-02)</small>

* Add the Blockcore Indexer ([90c2576](https://github.com/block-core/blockcore-indexer/commit/90c2576))
* Feature/build pipeline (#1) ([f450e3e](https://github.com/block-core/blockcore-indexer/commit/f450e3e)), closes [#1](https://github.com/block-core/blockcore-indexer/issues/1)
* Feature/docker workflow (#2) ([672f4d5](https://github.com/block-core/blockcore-indexer/commit/672f4d5)), closes [#2](https://github.com/block-core/blockcore-indexer/issues/2)
* Improve the setup of the Indexer, including build pipelines and deploy of Docker image (#3) ([adaf19f](https://github.com/block-core/blockcore-indexer/commit/adaf19f)), closes [#3](https://github.com/block-core/blockcore-indexer/issues/3) [#1](https://github.com/block-core/blockcore-indexer/issues/1)
* Initial commit ([d39d06a](https://github.com/block-core/blockcore-indexer/commit/d39d06a))
* Update docker.yml to use latest as tag ([0e70e69](https://github.com/block-core/blockcore-indexer/commit/0e70e69))



