> fly pg create

# ? Choose an app name (leave blank to generate one): fitedit-pg
# automatically selected personal organization: Doug
# Some regions require a paid plan (bom, fra, maa).
# See https://fly.io/plans to set up a plan.
#
# ? Select region: Atlanta, Georgia (US) (atl)
# ? Select configuration: Production (High Availability) - 3 nodes, 2x shared CPUs, 4GB RAM, 40GB disk
# Creating postgres cluster in organization personal
# Creating app...
# Setting secrets on app fitedit-pg...
# Provisioning 1 of 3 machines with image flyio/postgres-flex:15.3@sha256:c380a6108f9f49609d64e5e83a3117397ca3b5c3202d0bf0996883ec3dbb80c8
# Waiting for machine to start...
# Machine 5683dd71ad94d8 is created
# Provisioning 2 of 3 machines with image flyio/postgres-flex:15.3@sha256:c380a6108f9f49609d64e5e83a3117397ca3b5c3202d0bf0996883ec3dbb80c8
# Waiting for machine to start...
# Machine 5683dd9ec73e78 is created
# Provisioning 3 of 3 machines with image flyio/postgres-flex:15.3@sha256:c380a6108f9f49609d64e5e83a3117397ca3b5c3202d0bf0996883ec3dbb80c8
# Waiting for machine to start...
# Machine 1781119ea93d18 is created
# ==> Monitoring health checks
#   Waiting for 5683dd71ad94d8 to become healthy (started, 3/3)
#   Waiting for 5683dd9ec73e78 to become healthy (started, 3/3)
#   Waiting for 1781119ea93d18 to become healthy (started, 3/3)
#
# Postgres cluster fitedit-pg created
#   Username:    postgres
#   Password:    6dPejchOFnOK1Gw
#   Hostname:    fitedit-pg.internal
#   Flycast:     fdaa:2:7ab8:0:1::2
#   Proxy port:  5432
#   Postgres port:  5433
#   Connection string: postgres://postgres:6dPejchOFnOK1Gw@fitedit-pg.flycast:5432
#

> fly pg attach --app fitedit fitedit-pg
# Checking for existing attachments
# Registering attachment
# Creating database
# Creating user
#
# Postgres cluster fitedit-pg is now attached to fitedit
# The following secret was added to fitedit:
#   DATABASE_URL=postgres://fitedit:QYIzjYe7KNTDwJA@fitedit-pg.flycast:5432/fitedit?sslmode=disable
