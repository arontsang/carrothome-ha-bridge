#!/usr/bin/env bashio

bashio::log.info "Preparing to start..."

Carrot__Server=$(bashio::config 'Carrot.Server')
Carrot__User=$(bashio::config 'Carrot.User')
Carrot__Pass=$(bashio::config 'Carrot.Pass')

export Carrot__Server
export Carrot__User
export Carrot__Pass

if bashio::config.false 'Http.Validate_Https'; then
    bashio::log.yellow "HTTPS Validation is Disabled"
    Http__Validate_Https=false
    export Http__Validate_Https
fi

if bashio::config.is_empty 'mqtt' && bashio::var.has_value "$(bashio::services 'mqtt')"; then

    if bashio::var.true "$(bashio::services 'mqtt' 'ssl')"; then
        Mqtt__UseTls=true
        export Mqtt__UseTls
    fi
    Mqtt__Server=$(bashio::services 'mqtt' 'host')
    Mqtt__Pass=$(bashio::services 'mqtt' 'password')
    Mqtt__User=$(bashio::services 'mqtt' 'username')

    export Mqtt__Server
    export Mqtt__Pass
    export Mqtt__User
fi

dotnet /app/CarrotHome.Mqtt.dll