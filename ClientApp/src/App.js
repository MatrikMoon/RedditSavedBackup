import React, { Component } from 'react';

export default class App extends Component {
    static displayName = App.name;

    render() {
        return (
            <div>
                <a href="/save/signin" className="btn btn-primary">Link to Reddit</a>
            </div>
        );
    }

    async populateWeatherData() {
        const response = await fetch('AuthenticateWithReddit');
        console.log(response);
    }
}
